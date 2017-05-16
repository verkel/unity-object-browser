using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DebugObjectBrowser {
	public class ObjectBrowser {
		private static ObjectBrowser _instance;

		public static ObjectBrowser instance {
			get {
				if (_instance == null) {
					_instance = new ObjectBrowser();
				}
				return _instance;
			}
		}

		private static readonly GUILayoutOption[] FieldListButtonLayout = { GUILayout.MinWidth(75) };
		private static readonly GUILayoutOption[] BreadcrumbButtonLayout = { GUILayout.MinWidth(75), GUILayout.ExpandWidth(false) };
		private static readonly GUILayoutOption[] UpdateIntervalSliderLayout = { GUILayout.MinWidth(200) };
		private static readonly ITypeHandler LeafTypeHandler = new BasicLeafTypeHandler();
		private static readonly ITypeHandler EnumerableHandler = new EnumerableHandler();

		private GUIStyle fieldListValueLabelStyle;
		private GUIStyle FieldListValueLabelStyle {
			get {
				if (fieldListValueLabelStyle == null) {
					fieldListValueLabelStyle = new GUIStyle(GUI.skin.label) { wordWrap = false };
				}
				return fieldListValueLabelStyle;
			}
		}

		private List<Element> path = new List<Element>();
		private List<object> root = new List<object>();
		private List<Element> childrenCache = new List<Element>();
		private bool childrenCached = false;
		private float childrenCacheTime = 0f;
		private float childrenUpdateInterval = 0.1f;
		private Vector2 scrollPos = new Vector2();
		private float listItemHeight = 20;
		private static int listItemCount = 1;
		private IDictionary<Type, ITypeHandler> registeredHandlers = new Dictionary<Type, ITypeHandler>();
		private IDictionary<Type, ITypeHandler> typeToHandler = new Dictionary<Type, ITypeHandler>();
		private Action action = null;

		#region Public API

		public void Add(object obj) {
			root.Add(obj);
		}

		public void Remove(object obj) {
			root.Remove(obj);
		}

		public void RegisterHandler(Type type, ITypeHandler handler) {
			registeredHandlers[type] = handler;
		}

		public void DrawGui() {
			MaybeClearChildrenCache();
			DrawUpdateIntervalSlider();
			DrawBreadcrumb();
			DrawFieldList();
			DoAction();
		}

		#endregion

		private ObjectBrowser() {
			AddRootElement();
			RegisterBuiltinHandlers();
		}

		private void RegisterBuiltinHandlers() {
			RegisterHandler(typeof(object), new ObjectHandler());
			foreach (var primitiveType in TypeUtil.Primitives) {
				RegisterHandler(primitiveType, LeafTypeHandler);
			}
			RegisterHandler(typeof(Enum), LeafTypeHandler);
			RegisterHandler(typeof(string), LeafTypeHandler);
			RegisterHandler(typeof(ICollection), EnumerableHandler);
			RegisterHandler(typeof(IShowAsList), EnumerableHandler);

			RegisterHandler(typeof(GameObject), new GameObjectHandler());
			RegisterHandler(typeof(Scene), new SceneHandler());
		}

		private void AddRootElement() {
			var rootElem = Element.Create(root, "Objects");
			rootElem.breadcrumbText = "Objects";
			path.Add(rootElem);
		}

		private void DrawUpdateIntervalSlider() {
			GUILayout.BeginHorizontal();
			GUILayout.Label("Update interval: ");
			GUILayout.BeginVertical();
			GUILayout.Space(10f);
			childrenUpdateInterval = GUILayout.HorizontalSlider(childrenUpdateInterval, 0.01f, 1f, UpdateIntervalSliderLayout);
			GUILayout.EndVertical();
			GUILayout.Label(childrenUpdateInterval.ToString());
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		private void DrawFieldList() {
			DrawFieldList(path.Last().obj);
		}

		private void DoAction() {
			if (action != null) {
				action();
				action = null;
			}
		}

		private void DrawFieldList(object parent) {
			scrollPos = GUILayout.BeginScrollView(scrollPos, GUI.skin.box, GUILayout.Width(Screen.width));
			GUILayout.BeginHorizontal();
			DoDrawFieldList(parent, scrollPos);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndScrollView();
			UpdateFieldListItemCount();
		}

		private void UpdateFieldListItemCount() {
			if (Event.current.type == EventType.Repaint) {
				Rect rekt = GUILayoutUtility.GetLastRect();
				listItemCount = (int)(rekt.height / listItemHeight);
			}
		}

		private void DoDrawFieldList(object parent, Vector2 scrollPos) {
			var parentHandler = GetHandler(parent);
			var children = GetChildren(parent, parentHandler);
			listItemHeight = GUI.skin.button.CalcHeight(new GUIContent("Test button content"), float.MaxValue);
			int firstIndex = (int)(scrollPos.y / listItemHeight);
			firstIndex = Mathf.Clamp(firstIndex, 0, Mathf.Max(0, children.Count - listItemCount));
			DrawFieldListColumn(children, firstIndex, DrawFieldListButton);
			DrawFieldListColumn(children, firstIndex, DrawFieldListValueLabel);
		}

		private delegate void DrawFieldListCellDelegate(Element element, object obj, ITypeHandler handler);

		private void DrawFieldListColumn(IList<Element> children, int firstIndex, 
				DrawFieldListCellDelegate drawCell) {
			GUILayout.BeginVertical();
			GUILayout.Space(firstIndex * listItemHeight);
			for (int i = firstIndex; i < Mathf.Min(children.Count, firstIndex + listItemCount); i++) {
				var element = children[i];
				var obj = element.obj;
				var handler = GetHandler(obj);
				drawCell(element, obj, handler);
			}
			GUILayout.Space(Mathf.Max(0, (children.Count - firstIndex - listItemCount) * listItemHeight));
			GUILayout.EndVertical();
		}

		private void DrawFieldListValueLabel(Element element, object obj, ITypeHandler handler) {
			string valueText;
			if (element.type == Element.Type.ValueRow) {
				valueText = obj == null ? "null" : handler.GetStringValue(obj);
			}
			else {
				valueText = "";
			}
			GUILayout.Label(valueText, FieldListValueLabelStyle);
		}

		private void DrawFieldListButton(Element element, object obj, ITypeHandler handler) {
			var buttonText = element.text;
			GUI.enabled = !handler.IsLeaf(obj);
			if (element.type == Element.Type.ValueRow) {
				if (GUILayout.Button(buttonText, FieldListButtonLayout)) {
					action = () => SelectChild(element);
				}
			}
			else if (element.type == Element.Type.Header) {
				var color = GUI.color;
				GUI.color = element.textColor;
				GUILayout.Label(element.text);
				GUI.color = color;
			}
			GUI.enabled = true;
		}

		private void GoToAncestor(object parent) {
			while (path.Last().obj != parent) {
				path.RemoveAt(path.Count - 1);
			}
			ClearChildrenCache();
		}

		private void SelectChild(Element elem) {
			path.Add(elem);
			ClearChildrenCache();
		}

		private void DrawBreadcrumb() {
			GUILayout.BeginHorizontal();
			using (var pathElements = path.GetEnumerator()) {
				Element parentElem = new Element();
				while (pathElements.MoveNext()) {
					var elem = pathElements.Current;
					var obj = elem.obj;
					if (elem.breadcrumbText == null) {
						elem.CreateBreadcrumbText(GetHandler(parentElem.obj), parentElem.obj, elem);
					}
					if (GUILayout.Button(elem.breadcrumbText, BreadcrumbButtonLayout)) {
						action = () => GoToAncestor(obj);
					}
					parentElem = elem;
				}
			}
			GUILayout.EndHorizontal();
		}

		private ITypeHandler GetHandler(object obj) {
			if (obj == null) return LeafTypeHandler;

			var type = obj.GetType();
			ITypeHandler handler;
			if (typeToHandler.TryGetValue(type, out handler)) {
				return handler;
			}
			else {
				// Cache handler
				handler = ResolveHandler(type);
				typeToHandler[type] = handler;
				return handler;
			}
		}

		private ITypeHandler ResolveHandler(Type type) {
			ITypeHandler handler;

			// Try exact class match
			if (registeredHandlers.TryGetValue(type, out handler)) {
				return handler;
			}

			// Try interfaces and base interfaces
			var interfaces = type.GetInterfaces();
			foreach (var iface in interfaces) {
				handler = ResolveHandlerForTypeOrBaseTypes(iface);
				if (handler != null) return handler;
			}

			// Try base class types
			var baseType = type.BaseType;
			handler = ResolveHandlerForTypeOrBaseTypes(baseType);
			if (handler != null) return handler;

			// Fall back to object handler
			if (registeredHandlers.TryGetValue(typeof(object), out handler)) {
				return handler;
			}

			throw new InvalidOperationException("No ITypeHandler for object set");
		}

		private ITypeHandler ResolveHandlerForTypeOrBaseTypes(Type type) {
			ITypeHandler handler;
			while (type != null && type != typeof(object)) {
				if (registeredHandlers.TryGetValue(type, out handler)) {
					return handler;
				}
				type = type.BaseType;
			}
			return null;
		}

		private IList<Element> GetChildren(object parent, ITypeHandler parentHandler) {
			if (!childrenCached) {
				var enumerator = parentHandler.GetChildren(parent);
				while (enumerator.MoveNext()) {
					childrenCache.Add(enumerator.Current);
				}
				childrenCached = true;
			}

			return childrenCache;
		}

		private void ClearChildrenCache() {
			childrenCache.Clear();
			childrenCached = false;
		}

		private void MaybeClearChildrenCache() {
			if (Time.realtimeSinceStartup >= childrenCacheTime + childrenUpdateInterval) {
				childrenCacheTime = Time.realtimeSinceStartup;
				ClearChildrenCache();
			}
		}

	}
}