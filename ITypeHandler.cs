using System;
using System.Collections.Generic;

namespace DebugObjectBrowser {
	public interface ITypeHandler {
		string GetStringValue(object obj);
		string GetBreadcrumbText(object parent, Element elem);
		IEnumerator<Element> GetChildren(object obj);
		bool IsLeaf(object obj);
	}

	public struct Element {
		public string text;
		public string breadcrumbText;
		public object obj;

		public Element(object obj, string text) {
			this.obj = obj;
			this.text = text;
			this.breadcrumbText = null;
		}

		public void CreateBreadcrumbText(ITypeHandler parentHandler, object parent, Element elem) {
			if (breadcrumbText == null) {
				breadcrumbText = parentHandler.GetBreadcrumbText(parent, elem);
			}
		}
	}
}