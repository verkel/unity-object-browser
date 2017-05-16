using System;
using System.Collections;
using System.Collections.Generic;

namespace DebugObjectBrowser {
	public class EnumerableHandler : ITypeHandler {
		public string GetStringValue(object obj) {
			return obj.ToString();
		}

		public IEnumerator<Element> GetChildren(object obj) {
			IEnumerable enumerable = (IEnumerable) obj;
			return EnumerableEnumerator(enumerable);
		}

		private IEnumerator<Element> EnumerableEnumerator(IEnumerable enumerable) {
			var inner = enumerable.GetEnumerator();
			int index = 0;
			while (inner.MoveNext()) {
				yield return Element.Create(inner.Current, index.ToString());
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