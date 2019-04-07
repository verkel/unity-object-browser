using System;
using System.Collections.Generic;
using UnityEngine;

namespace DebugObjectBrowser {
	public class GameObjectHandler : ITypeHandler {
		private List<Component> componentResults = new List<Component>();

		public string GetStringValue(object obj) {
			return obj.ToString();
		}

		public IEnumerator<Element> GetChildren(object obj, DisplayOption displayOptions) {
			componentResults.Clear();
			GameObject go = (GameObject) obj;
			go.GetComponents(componentResults);
			return ComponentAndChildrenEnumerator(go, componentResults);
		}

		private IEnumerator<Element> ComponentAndChildrenEnumerator(GameObject go, List<Component> components) {
			yield return Element.CreateHeader("Components", Color.magenta);
			for (int i = 0; i < components.Count; i++) {
				var component = components[i];
				yield return Element.Create(component, component.GetType().Name);
			}

			yield return Element.CreateHeader("Children", Color.blue);
			var tf = go.transform;
			for (int i = 0; i < tf.childCount; i++) {
				var child = tf.GetChild(i).gameObject;
				yield return Element.Create(child, child.name);
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