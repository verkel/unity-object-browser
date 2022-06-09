using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DebugObjectBrowser {
	public class ManySingletonList : IShowAsList {
		private readonly HashSet<Type> baseTypes;
		private readonly string instancePropertyName;

		private Data[] instanceProperties;
		private Assembly[] assemblies;

		public ManySingletonList(HashSet<Type> baseTypes, string instancePropertyName = "Instance", Assembly[] assemblies = null) {
			this.instancePropertyName = instancePropertyName;
			this.baseTypes = baseTypes;
			this.assemblies = assemblies;
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
				var types = assemblies.SelectMany(assembly => assembly.GetTypes());
				instanceProperties =
					(from type in types
					let bt = type.BaseType
					where bt != null && !type.IsAbstract && bt.IsGenericType && baseTypes.Contains(bt.GetGenericTypeDefinition())
					orderby type.Name
					select new Data(type.Name, bt.GetProperty(instancePropertyName)))
					.ToArray();
			}
			return instanceProperties;
		}

		public override string ToString() {
			return string.Format("Singletons");
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
