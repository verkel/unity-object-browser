using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace DebugObjectBrowser {
	public class SceneHandler : ITypeHandler {
		public string GetStringValue(object obj) {
			return ((Scene) obj).name;
		}

		public string GetBreadcrumbText(object parent, Element elem) {
			return elem.text;
		}

		public IEnumerator<Element> GetChildren(object obj) {
			var scene = (Scene)obj;
			return SceneObjectsEnumerator(scene);
		}

		private IEnumerator<Element> SceneObjectsEnumerator(Scene scene) {
			foreach (var go in scene.GetRootGameObjects()) {
				yield return Element.Create(go, go.name);
			}
		}

		public bool IsLeaf(object obj) {
			return false;
		}
	}
}