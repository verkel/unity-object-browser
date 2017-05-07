using System;
using System.Collections.Generic;

namespace DebugObjectBrowser {
	public interface ITypeHandler {
		Type GetHandledType();
		string GetStringValue(object obj);
		IEnumerator<Element> GetChildren(object obj);
	}

	public struct Element {
		public string text;
		public object obj;

		public Element(object obj, string text) {
			this.obj = obj;
			this.text = text;
		}
	}
}