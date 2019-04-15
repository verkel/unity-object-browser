using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DebugObjectBrowser {
	public class ObjectHandler : ITypeHandler {
		private const string BackingFieldSuffix = "k__BackingField";
		private static readonly FieldInfoComparer FieldInfoComparer = new FieldInfoComparer();
		private static readonly PropertyInfoComparer PropertyInfoComparer = new PropertyInfoComparer();
		private static readonly FieldInfoEqualityComparer FieldInfoEqualityComparer = new FieldInfoEqualityComparer();

		private readonly IDictionary<Type, FieldInfo[]> typeToFieldInfos = new Dictionary<Type, FieldInfo[]>();
		private readonly IDictionary<Type, TypeProperties> typeToProperties = new Dictionary<Type, TypeProperties>();

		public string GetStringValue(object obj) {
			return obj.ToString();
		}

		public IEnumerator<Element> GetChildren(object obj, DisplayOption displayOptions) {
			if (displayOptions.IsSet(DisplayOption.Fields) || displayOptions.IsSet(DisplayOption.BackingFields)) {
				foreach (var field in GetFields(obj, displayOptions)) yield return field;
			}

			if (displayOptions.IsSet(DisplayOption.Properties))
				foreach (var prop in GetProperties(obj)) yield return prop;
		}

		public void ClearFieldInfoCache() {
			typeToFieldInfos.Clear();
		}

		private IEnumerable<Element> GetFields(object obj, DisplayOption displayOptions) {
			var type = obj.GetType();
			FieldInfo[] fieldInfos;
			if (!typeToFieldInfos.TryGetValue(type, out fieldInfos)) {
				fieldInfos = GetFieldsIncludingBaseClasses(type, displayOptions,
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				Array.Sort(fieldInfos, FieldInfoComparer);
				typeToFieldInfos[type] = fieldInfos;
			}
			return FieldsEnumerator(fieldInfos, obj);
		}

		private IEnumerable<Element> FieldsEnumerator(FieldInfo[] fieldInfos, object obj) {
			yield return Element.CreateHeader("Fields", Color.cyan);
			for (int i = 0; i < fieldInfos.Length; i++) {
				var fieldInfo = fieldInfos[i];
				var name = RemovePropertyBackingFieldSuffix(fieldInfo.Name);
				yield return Element.Create(fieldInfo.GetValue(obj), name);
			}
		}

		private static string RemovePropertyBackingFieldSuffix(string name) {
			var index = name.LastIndexOf(BackingFieldSuffix, StringComparison.Ordinal);
			if (index != -1) name = name.Substring(0, index);
			return name;
		}

		private IEnumerable<Element> GetProperties(object obj) {
			var type = obj.GetType();
			TypeProperties typeProperties;
			if (!typeToProperties.TryGetValue(type, out typeProperties)) {
				var propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				Array.Sort(propertyInfos, PropertyInfoComparer);
				typeProperties = new TypeProperties(propertyInfos);
				typeToProperties[type] = typeProperties;
			}
			return PropertiesEnumerator(typeProperties, obj);
		}

		private IEnumerable<Element> PropertiesEnumerator(TypeProperties typeProperties, object obj) {
			yield return Element.CreateHeader("Properties", Color.green);
			var propertyInfos = typeProperties.propertyInfos;
			for (int i = 0; i < propertyInfos.Length; i++) {
				var propertyInfo = propertyInfos[i];
				object value;
				var propertyName = propertyInfo.Name;
				
				// Try invoking the property getter, if an exception occurs, do not call it again
				if (typeProperties.IsSkipped(propertyName)) {
					value = typeProperties.GetSkipReason(propertyName);
				}
				else {
					try {
						value = propertyInfo.GetValue(obj);
					}
					catch (Exception e) {
						e = e.InnerException ?? e;
						typeProperties.AddSkipped(propertyName, e);
						value = typeProperties.GetSkipReason(propertyName);
					}
				}
				
				yield return Element.Create(value, propertyInfo.Name);
			}
		}

		public bool IsLeaf(object obj) {
			return false;
		}

		public string GetBreadcrumbText(object parent, Element elem) {
			return parent.GetType().Name + "." + elem.text;
		}
		
		// https://stackoverflow.com/questions/9201859/why-doesnt-type-getfields-return-backing-fields-in-a-base-class
		private static FieldInfo[] GetFieldsIncludingBaseClasses(Type type, DisplayOption displayOptions, BindingFlags bindingFlags)
		{
			FieldInfo[] fieldInfos = type.GetFields(bindingFlags);
			var fields = displayOptions.IsSet(DisplayOption.Fields);
			var backingFields = displayOptions.IsSet(DisplayOption.BackingFields);

			// If this class doesn't have a base, don't waste any time
			if (type.BaseType == typeof(object))
			{
				return fieldInfos;
			}
			else
			{   // Otherwise, collect all types up to the furthest base class
				var currentType = type;
				var fieldInfoList = new HashSet<FieldInfo>(fieldInfos, FieldInfoEqualityComparer);
				while (currentType != typeof(object))
				{
					fieldInfos = currentType.GetFields(bindingFlags);
					fieldInfoList.UnionWith(fieldInfos);
					currentType = currentType.BaseType;
				}

				if (!backingFields) {
					fieldInfoList.RemoveWhere(info => info.Name.EndsWith(BackingFieldSuffix));
				}

				if (!fields) {
					fieldInfoList.RemoveWhere(info => !info.Name.EndsWith(BackingFieldSuffix));
				}
				
				return fieldInfoList.ToArray();
			}
		}
	}

	class FieldInfoComparer : IComparer<FieldInfo> {
		public int Compare(FieldInfo x, FieldInfo y) {
			int typeCmp = GetTypeOrdinal(x).CompareTo(GetTypeOrdinal(y));
			if (typeCmp != 0) return typeCmp;
			int nameCmp = x.Name.CompareTo(y.Name);
			return nameCmp;
		}

		private int GetTypeOrdinal(FieldInfo info) {
			return info.FieldType.IsValueType ? 1 : 0;
		}
	}
	
	class FieldInfoEqualityComparer : IEqualityComparer<FieldInfo>
	{
		public bool Equals(FieldInfo x, FieldInfo y)
		{
			return x.DeclaringType == y.DeclaringType && x.Name == y.Name;
		}

		public int GetHashCode(FieldInfo obj)
		{
			return obj.Name.GetHashCode() ^ obj.DeclaringType.GetHashCode();
		}
	}
	
	class PropertyInfoComparer : IComparer<PropertyInfo> {
		public int Compare(PropertyInfo x, PropertyInfo y) {
			int typeCmp = GetTypeOrdinal(x).CompareTo(GetTypeOrdinal(y));
			if (typeCmp != 0) return typeCmp;
			int nameCmp = x.Name.CompareTo(y.Name);
			return nameCmp;
		}

		private int GetTypeOrdinal(PropertyInfo info) {
			return info.PropertyType.IsValueType ? 1 : 0;
		}
	}

	class TypeProperties {
		public readonly PropertyInfo[] propertyInfos;
		private Dictionary<string, string> skippedProperties;

		public TypeProperties(PropertyInfo[] propertyInfos) {
			this.propertyInfos = propertyInfos;
		}

		public bool IsSkipped(string propertyName) {
			return skippedProperties != null && skippedProperties.ContainsKey(propertyName);
		}

		public string GetSkipReason(string propertyName) {
			return skippedProperties[propertyName];
		}

		public void AddSkipped(string propertyName, Exception ex) {
			if (skippedProperties == null) skippedProperties = new Dictionary<string, string>();
			skippedProperties.Add(propertyName, ex.GetType().Name);
		}
	}
}