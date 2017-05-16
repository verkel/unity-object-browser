using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace DebugObjectBrowser {
	public class SceneManagerWrapper : IShowAsList {
		public static SceneManagerWrapper instance = new SceneManagerWrapper();

		IEnumerator IEnumerable.GetEnumerator() {
			return ScenesEnumerator();
		}

		private IEnumerator<Scene> ScenesEnumerator() {
			for (int i = 0; i < SceneManager.sceneCount; i++) {
				yield return SceneManager.GetSceneAt(i);
			}
		}

		public override string ToString() {
			return "Scenes";
		}
	}
}