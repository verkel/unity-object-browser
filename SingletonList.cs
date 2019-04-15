using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DebugObjectBrowser {
	public class SingletonList : IShowAsList {
		private readonly Type baseType;
		private readonly string instancePropertyName;

		private List<KeyValuePair<string, PropertyInfo>> instanceProperties;

		public SingletonList(Type baseType, string instancePropertyName = "Instance") {
			this.instancePropertyName = instancePropertyName;
			this.baseType = baseType;
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return SingletonsEnumerator();
		}

		private IEnumerator<object> SingletonsEnumerator() {
			var pairs = GetInstanceProperties();
			for (var i = 0; i < pairs.Count; i++) {
				object propValue = null;

				// Try getting the singleton value while we haven't got an exception
				if (pairs[i].Value != null) {
					try {
						propValue = pairs[i].Value.GetValue(null);
					}
					catch (Exception e) {
						e = e.InnerException ?? e;
						pairs[i] = new KeyValuePair<string, PropertyInfo>(pairs[i].Key + ": " + e.GetType().Name, null);
					}
				}

				// Exception received, ignore this singleton from now on and display the exception name
				if (pairs[i].Value == null) {
					propValue = pairs[i].Key;
				}

				yield return propValue;
			}
		}

		private List<KeyValuePair<string, PropertyInfo>> GetInstanceProperties() {
			if (instanceProperties == null) {
				instanceProperties =
					(from type in baseType.Assembly.GetTypes()
					let bt = type.BaseType
					where bt != null && bt.IsGenericType && bt.GetGenericTypeDefinition() == baseType
					orderby type.FullName
					select new KeyValuePair<string, PropertyInfo>(type.Name, bt.GetProperty(instancePropertyName)))
					.ToList();
			}
			return instanceProperties;
		}

		public override string ToString() {
			return string.Format("Singletons ({0})", baseType.Name);
		}
	}
}