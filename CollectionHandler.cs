using System;
using System.Collections;
using System.Collections.Generic;

namespace DebugObjectBrowser {
	public class CollectionHandler : ITypeHandler {
		public string GetStringValue(object obj) {
			return obj.ToString();
		}

		public IEnumerator<Element> GetChildren(object obj) {
			ICollection collection = (ICollection) obj;
			return CollectionEnumerator(collection);
		}

		private IEnumerator<Element> CollectionEnumerator(ICollection collection) {
			var inner = collection.GetEnumerator();
			int index = 0;
			while (inner.MoveNext()) {
				yield return new Element(inner.Current, index.ToString());
				index++;
			}
		}

		public Type GetHandledType() {
			return typeof(ICollection);
		}

		public bool IsLeaf(object obj) {
			return false;
		}

		public string GetBreadcrumbText(object parent, Element elem) {
			return elem.text + ": " + elem.obj.GetType().Name;
		}
	}
}