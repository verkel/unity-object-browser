using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DebugObjectBrowser {
	public class SingletonList : IShowAsList {
		private readonly Type baseType;
		private readonly string instancePropertyName;

		private List<PropertyInfo> instanceProperties;

		public SingletonList(Type baseType, string instancePropertyName = "Instance") {
			this.instancePropertyName = instancePropertyName;
			this.baseType = baseType;
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return SingletonsEnumerator();
		}

		private IEnumerator<object> SingletonsEnumerator() {
			foreach (var property in GetInstanceProperties()) {
				object value;
				try {
					value = property.GetValue(null);
				}
				catch (Exception e) {
					value = e.GetType().Name;
				}
				yield return value;
			}
		}

		private IEnumerable<PropertyInfo> GetInstanceProperties() {
			if (instanceProperties == null) {
				instanceProperties = baseType.Assembly.GetTypes()
					.Select(type => type.BaseType)
					.Where(bt => bt != null && bt.IsGenericType && bt.GetGenericTypeDefinition() == baseType)
					.Select(bt => bt.GetProperty(instancePropertyName))
					.ToList();
			}
			return instanceProperties;
		}

		public override string ToString() {
			return string.Format("Singletons ({0})", baseType.Name);
		}
	}
}