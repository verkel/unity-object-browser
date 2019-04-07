using System;
using System.Collections.Generic;
using UnityEngine;

namespace DebugObjectBrowser {
	public interface ITypeHandler {
		string GetStringValue(object obj);
		string GetBreadcrumbText(object parent, Element elem);
		IEnumerator<Element> GetChildren(object obj, DisplayOption displayOptions);
		bool IsLeaf(object obj);
	}

	public struct Element {
		public enum Type {
			ValueRow, Header 
		}

		public Type type;
		public string text;
		public Color textColor;
		public string breadcrumbText;
		public object obj;

		public static Element Create(object obj, string text) {
			return new Element {
				type = Type.ValueRow,
				obj = obj,
				text = text,
				breadcrumbText = null
			};
		}

		public static Element CreateHeader(string headerText, Color textColor) {
			return new Element {
				type = Type.Header,
				text = headerText,
				textColor = textColor
			};
		}

		public void CreateBreadcrumbText(ITypeHandler parentHandler, object parent, Element elem) {
			if (breadcrumbText == null) {
				breadcrumbText = parentHandler.GetBreadcrumbText(parent, elem);
			}
		}
	}
}