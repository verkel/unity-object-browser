using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DebugObjectBrowser {
	public class ObjectHandler : ITypeHandler {
		private readonly IDictionary<Type, FieldInfo[]> typeToFieldInfos
			= new Dictionary<Type, FieldInfo[]>();

		public string GetStringValue(object obj) {
			return obj.ToString();
		}

		public IEnumerator<Element> GetChildren(object obj) {
			var type = obj.GetType();
			FieldInfo[] fieldInfos;
			if (!typeToFieldInfos.TryGetValue(type, out fieldInfos)) {
				fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
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

		public Type GetHandledType() {
			return typeof(object);
		}
	}
}