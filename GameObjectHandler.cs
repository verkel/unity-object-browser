using System;
using System.Collections.Generic;
using UnityEngine;

namespace DebugObjectBrowser {
	public class GameObjectHandler : ITypeHandler {
		private List<Component> componentResults = new List<Component>();

		public string GetStringValue(object obj) {
			return obj.ToString();
		}

		public IEnumerator<Element> GetChildren(object obj) {
			componentResults.Clear();
			GameObject go = (GameObject) obj;
			go.GetComponents(componentResults);
			return ComponentEnumerator(componentResults);
		}

		private IEnumerator<Element> ComponentEnumerator(List<Component> components) {
			for (int i = 0; i < components.Count; i++) {
				var component = components[i];
				yield return new Element(component, component.GetType().Name);
			}
		}

		public bool IsLeaf(object obj) {
			return false;
		}

		public string GetBreadcrumbText(object parent, Element elem) {
			return parent.GetType().Name + "." + elem.text;
		}
	}
}