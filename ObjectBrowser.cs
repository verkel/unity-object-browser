using System;
using System.Collections;
using System.Collections.Generic;
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

		private Stack<object> path = new Stack<object>();
		private List<object> root = new List<object>();
		private Vector2 scrollPos = new Vector2();
		private IDictionary<Type, ITypeHandler> registeredHandlers = new Dictionary<Type, ITypeHandler>();
		private IDictionary<Type, ITypeHandler> typeToHandler = new Dictionary<Type, ITypeHandler>();

		private ObjectBrowser() {
			path.Push(root);
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
			DrawColumns();
		}

		private void DrawColumns() {
			GUILayout.BeginHorizontal();
			using (var pathElements = path.GetEnumerator()) {
				while (pathElements.MoveNext()) {
					var element = pathElements.Current;
					DrawColumn(element);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawColumn(object parent) {
			GUILayout.BeginVertical();
			var parentHandler = GetHandler(parent);
			var enumerator = parentHandler.GetChildren(parent);
			while (enumerator.MoveNext()) {
				var obj = enumerator.Current;
				var handler = GetHandler(obj);
				var text = handler.GetStringValue(obj);
				if (GUILayout.Button(text)) {
					SetSelection(parent, obj);
				}
			}
			GUILayout.EndVertical();
		}

		private void SetSelection(object parent, object obj) {
			while (path.Peek() != parent) {
				path.Pop();
			}
			path.Push(obj);
		}

		private void DrawBreadcrumb() {
			GUILayout.BeginHorizontal();
			GUILayout.Label("Selection: ");
			using (var pathElements = path.GetEnumerator()) {
				while (pathElements.MoveNext()) {
					var obj = pathElements.Current;
					var handler = GetHandler(obj);
					var text = handler.GetStringValue(obj);
					GUILayout.Button(text);
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