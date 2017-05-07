using System;
using System.Collections;

namespace DebugObjectBrowser {
	public class CollectionHandler : ITypeHandler {
		public string GetStringValue(object obj) {
			return obj.ToString();
		}

		public IEnumerator GetChildren(object obj) {
			ICollection collection = (ICollection)obj;
			return collection.GetEnumerator();
		}

		public Type GetHandledType() {
			return typeof(ICollection);
		}
	}
}