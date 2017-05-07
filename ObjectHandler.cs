using System;
using System.Collections.Generic;
using System.Reflection;

namespace DebugObjectBrowser {
	public class ObjectHandler : ITypeHandler {
		private static readonly FieldInfoComparer fieldInfoComparer = new FieldInfoComparer();

		private readonly IDictionary<Type, FieldInfo[]> typeToFieldInfos
			= new Dictionary<Type, FieldInfo[]>();

		public string GetStringValue(object obj) {
			return obj.ToString();
		}

		public IEnumerator<Element> GetChildren(object obj) {
			var type = obj.GetType();
			FieldInfo[] fieldInfos;
			if (!typeToFieldInfos.TryGetValue(type, out fieldInfos)) {
				fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				Array.Sort(fieldInfos, fieldInfoComparer);
				typeToFieldInfos[type] = fieldInfos;
			}
			return FieldsEnumerator(fieldInfos, obj);
		}

		private IEnumerator<Element> FieldsEnumerator(FieldInfo[] fieldInfos, object obj) {
			for (int i = 0; i < fieldInfos.Length; i++) {
				var fieldInfo = fieldInfos[i];
				yield return new Element(fieldInfo.GetValue(obj), fieldInfo.Name);
			}
		}

		public bool IsLeaf(object obj) {
			return false;
		}

		public string GetBreadcrumbText(object parent, Element elem) {
			return parent.GetType().Name + "." + elem.text;
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
}