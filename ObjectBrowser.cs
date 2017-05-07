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

		private List<object> path = new List<object>();
		private List<object> root = new List<object>();
		private Vector2 scrollPos = new Vector2();
		private IDictionary<Type, ITypeHandler> registeredHandlers = new Dictionary<Type, ITypeHandler>();
		private IDictionary<Type, ITypeHandler> typeToHandler = new Dictionary<Type, ITypeHandler>();
		private Action action = null;

		private ObjectBrowser() {
			path.Add(root);
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
			DrawFieldList(path.Last());
		}

		private void DoAction() {
			if (action != null) action();
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
				if (GUILayout.Button(buttonText, GUILayout.MinWidth(75))) {
					action = () => SetSelection(parent, element.obj);
				}
			}
			GUILayout.EndVertical();
		}

		private void SetSelection(object parent, object obj = null) {
			while (path.Last() != parent) {
				path.Pop();
			}
			if (obj != null) {
				path.Add(obj);
			}
		}

		private void DrawBreadcrumb() {
			GUILayout.BeginHorizontal();
			using (var pathElements = path.GetEnumerator()) {
				while (pathElements.MoveNext()) {
					var obj = pathElements.Current;
					var handler = GetHandler(obj);
					var text = handler.GetStringValue(obj);
					if (GUILayout.Button(text, GUILayout.ExpandWidth(false))) {
						action = () => SetSelection(obj);
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