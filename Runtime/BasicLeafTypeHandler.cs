using System;
using System.Collections.Generic;

namespace DebugObjectBrowser {
	public class BasicLeafTypeHandler : ITypeHandler {
		public string GetStringValue(object obj) {
			return obj.ToString();
		}

		public IEnumerator<Element> GetChildren(object obj, DisplayOption displayOptions) {
			throw new NotImplementedException();
		}

		public bool IsLeaf(object obj) {
			return true;
		}

		public string GetBreadcrumbText(object parent, Element elem) {
			throw new NotImplementedException();
		}
	}
}