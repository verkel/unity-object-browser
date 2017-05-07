using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Boomlagoon.TextFx.JSON;
using UnityEngine;

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

		private readonly GUILayoutOption[] FieldListButtonLayout = { GUILayout.MinWidth(75) };
		private readonly GUILayoutOption[] BreadcrumbButtonLayout = { GUILayout.MinWidth(75), GUILayout.ExpandWidth(false) };

		private List<Element> path = new List<Element>();
		private List<object> root = new List<object>();
		private Vector2 scrollPos = new Vector2();
		private IDictionary<Type, ITypeHandler> registeredHandlers = new Dictionary<Type, ITypeHandler>();
		private IDictionary<Type, ITypeHandler> typeToHandler = new Dictionary<Type, ITypeHandler>();
		private Action action = null;

		private ObjectBrowser() {
			path.Add(new Element(root, "Objects"));
			RegisterHandler(new ObjectHandler());
			RegisterHandler(new CollectionHandler());
		}

		public void Add(object obj) {
			root.Add(obj);
		}

		public void Remove(object obj) {
			root.Remove(obj);
		}

		public void RegisterHandler(ITypeHandler handler) {
			registeredHandlers[handler.GetHandledType()] = handler;
		}

		public void DrawGui() {
			DrawBreadcrumb();
			DrawFieldList();
			DoAction();
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
			scrollPos = GUILayout.BeginScrollView(scrollPos, GUI.skin.box);
			GUILayout.BeginHorizontal();
			DoDrawFieldList(parent);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndScrollView();
		}

		private void DoDrawFieldList(object parent) {
			var parentHandler = GetHandler(parent);
			DrawFieldListButtonsColumn(parent, parentHandler);
			DrawFieldListValuesColumn(parent, parentHandler);
		}

		private void DrawFieldListValuesColumn(object parent, ITypeHandler parentHandler) {
			GUILayout.BeginVertical();
			IEnumerator<Element> enumerator = parentHandler.GetChildren(parent);
			while (enumerator.MoveNext()) {
				var element = enumerator.Current;
				var obj = element.obj;
				var handler = GetHandler(element);
				var valueText = obj == null ? "null" : handler.GetStringValue(obj);
				GUILayout.Label(valueText);
			}
			GUILayout.EndVertical();
		}

		private void DrawFieldListButtonsColumn(object parent, ITypeHandler parentHandler) {
			var enumerator = parentHandler.GetChildren(parent);
			GUILayout.BeginVertical();
			while (enumerator.MoveNext()) {
				var element = enumerator.Current;
				var buttonText = element.text;
				if (GUILayout.Button(buttonText, FieldListButtonLayout)) {
					action = () => SelectChild(element);
				}
			}
			GUILayout.EndVertical();
		}

		private void GoToAncestor(object parent) {
			while (path.Last().obj != parent) {
				path.Pop();
			}
		}

		private void SelectChild(Element elem) {
			path.Add(elem);
		}

		private void DrawBreadcrumb() {
			GUILayout.BeginHorizontal();
			using (var pathElements = path.GetEnumerator()) {
				while (pathElements.MoveNext()) {
					var elem = pathElements.Current;
					var obj = elem.obj;
					var text = elem.text;
					if (GUILayout.Button(text, BreadcrumbButtonLayout)) {
						action = () => GoToAncestor(obj);
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		private ITypeHandler GetHandler(object obj) {
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
	}
}