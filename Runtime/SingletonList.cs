using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DebugObjectBrowser {
	public class SingletonList : IShowAsList {
		private readonly Type baseType;
		private readonly string instancePropertyName;

		private Data[] instanceProperties;

		public SingletonList(Type baseType, string instancePropertyName = "Instance") {
			this.instancePropertyName = instancePropertyName;
			this.baseType = baseType;
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return SingletonsEnumerator();
		}

		private IEnumerator<Element> SingletonsEnumerator() {
			var properties = GetInstanceProperties();
			for (var i = 0; i < properties.Length; i++) {
				var data = properties[i];
				object value = null;

				if (data.skipReason == null) {
					try {
						value = data.instanceProperty.GetValue(null, null);
					}
					catch (Exception e) {
						e = e.InnerException ?? e;
						data.skipReason = e.GetType().Name;
						properties[i] = data;
					}
				}

				if (data.skipReason != null) {
					value = data.skipReason;
				}

				yield return Element.Create(value, data.typeName);
			}
		}

		private Data[] GetInstanceProperties() {
			if (instanceProperties == null) {
				instanceProperties =
					(from type in baseType.Assembly.GetTypes()
					let bt = type.BaseType
					where bt != null && bt.IsGenericType && bt.GetGenericTypeDefinition() == baseType
					orderby type.FullName
					select new Data(type.Name, bt.GetProperty(instancePropertyName)))
					.ToArray();
			}
			return instanceProperties;
		}

		public override string ToString() {
			return string.Format("Singletons ({0})", baseType.Name);
		}

		private struct Data {
			public readonly string typeName;
			public readonly PropertyInfo instanceProperty;
			public string skipReason;

			public Data(string typeName, PropertyInfo instanceProperty) : this() {
				this.typeName = typeName;
				this.instanceProperty = instanceProperty;
			}
		}
	}
}