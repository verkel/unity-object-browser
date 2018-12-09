using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DebugObjectBrowser {
	public class ObjectHandler : ITypeHandler {
		private static readonly FieldInfoComparer fieldInfoComparer = new FieldInfoComparer();
		private static readonly FieldInfoEqualityComparer fieldInfoEqualityComparer = new FieldInfoEqualityComparer();

		private readonly IDictionary<Type, FieldInfo[]> typeToFieldInfos
			= new Dictionary<Type, FieldInfo[]>();

		public string GetStringValue(object obj) {
			return obj.ToString();
		}

		public IEnumerator<Element> GetChildren(object obj) {
			var type = obj.GetType();
			FieldInfo[] fieldInfos;
			if (!typeToFieldInfos.TryGetValue(type, out fieldInfos)) {
				fieldInfos = GetFieldsIncludingBaseClasses(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				Array.Sort(fieldInfos, fieldInfoComparer);
				typeToFieldInfos[type] = fieldInfos;
			}
			return FieldsEnumerator(fieldInfos, obj);
		}

		private IEnumerator<Element> FieldsEnumerator(FieldInfo[] fieldInfos, object obj) {
			yield return Element.CreateHeader("Fields", Color.cyan);
			for (int i = 0; i < fieldInfos.Length; i++) {
				var fieldInfo = fieldInfos[i];
				yield return Element.Create(fieldInfo.GetValue(obj), fieldInfo.Name);
			}
		}

		public bool IsLeaf(object obj) {
			return false;
		}

		public string GetBreadcrumbText(object parent, Element elem) {
			return parent.GetType().Name + "." + elem.text;
		}
		
		// https://stackoverflow.com/questions/9201859/why-doesnt-type-getfields-return-backing-fields-in-a-base-class
		private static FieldInfo[] GetFieldsIncludingBaseClasses(Type type, BindingFlags bindingFlags)
		{
			FieldInfo[] fieldInfos = type.GetFields(bindingFlags);

			// If this class doesn't have a base, don't waste any time
			if (type.BaseType == typeof(object))
			{
				return fieldInfos;
			}
			else
			{   // Otherwise, collect all types up to the furthest base class
				var currentType = type;
				var fieldInfoList = new HashSet<FieldInfo>(fieldInfos, fieldInfoEqualityComparer);
				while (currentType != typeof(object))
				{
					fieldInfos = currentType.GetFields(bindingFlags);
					fieldInfoList.UnionWith(fieldInfos);
					currentType = currentType.BaseType;
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
}