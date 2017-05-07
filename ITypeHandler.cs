using System;
using System.Collections;

namespace DebugObjectBrowser {
	public interface ITypeHandler {
		Type GetHandledType();
		string GetStringValue(object obj);
		IEnumerator GetChildren(object obj);
	}
}