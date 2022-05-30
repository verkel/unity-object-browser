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

		private static readonly ITypeHandler LeafTypeHandler = new BasicLeafTypeHandler();
		private static readonly ITypeHandler EnumerableHandler = new EnumerableHandler();

		private readonly IDictionary<Type, ITypeHandler> registeredHandlers = new Dictionary<Type, ITypeHandler>();
		private readonly IDictionary<Type, ITypeHandler> typeToHandler = new Dictionary<Type, ITypeHandler>();

		private readonly List<object> root = new List<object>();
		private readonly ObjectHandler objectHandler = new ObjectHandler();
		
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

		#endregion

		internal List<object> GetRoot() {
			return root;
		}
		
		internal ITypeHandler GetHandler(object obj) {
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

		internal void ClearObjectHandlerFieldInfoCache() {
			objectHandler.ClearFieldInfoCache();
		}

		private ObjectBrowser() {
			RegisterBuiltinHandlers();
		}

		private void RegisterBuiltinHandlers() {
			RegisterHandler(typeof(object), objectHandler);
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