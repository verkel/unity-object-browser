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

		public IEnumerator GetChildren(object obj) {
			var type = obj.GetType();
			FieldInfo[] fieldInfos;
			if (!typeToFieldInfos.TryGetValue(type, out fieldInfos)) {
				fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
				typeToFieldInfos[type] = fieldInfos;
			}
			return fieldInfos.GetEnumerator();
		}

		public Type GetHandledType() {
			return typeof(object);
		}
	}
}