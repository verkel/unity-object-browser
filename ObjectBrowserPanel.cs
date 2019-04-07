using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DebugObjectBrowser {
	public class ObjectBrowserPanel {
		private static readonly GUILayoutOption[] FieldListButtonLayout = { GUILayout.MinWidth(75) };
		private static readonly GUILayoutOption[] BreadcrumbButtonLayout = { GUILayout.MinWidth(75), GUILayout.ExpandWidth(false) };
		private static readonly GUILayoutOption[] UpdateIntervalSliderLayout = { GUILayout.MinWidth(200) };
		
		private readonly ObjectBrowser model;
		private readonly bool editor;
		private readonly List<Element> path = new List<Element>();
		private readonly List<Element> childrenCache = new List<Element>();
		
		private Action action;
		private Vector2 scrollPos;
		private int listItemCount = 1;
		private bool childrenCached;
		private float childrenCacheTime;
		private float childrenUpdateInterval = 0.1f;
		private DisplayOption displayOptions = DisplayOption.Fields;
		
		private GUIStyle fieldListValueLabelStyle;
		private GUIStyle FieldListValueLabelStyle {
			get {
				if (fieldListValueLabelStyle == null) {
					var buttonSkin = GUI.skin.button;
					fieldListValueLabelStyle = new GUIStyle(GUI.skin.label) { 
						wordWrap = false, 
					};
					fieldListValueLabelStyle.margin.top = buttonSkin.margin.top;
					fieldListValueLabelStyle.margin.bottom = buttonSkin.margin.bottom;
					fieldListValueLabelStyle.padding.top = buttonSkin.padding.top;
					fieldListValueLabelStyle.padding.bottom = buttonSkin.padding.bottom;
				}
				return fieldListValueLabelStyle;
			}
		}

		private GUIStyle fieldListHeaderLabelStyle;
		private GUIStyle FieldListHeaderLabelStyle {
			get {
				if (fieldListHeaderLabelStyle == null) {
					fieldListHeaderLabelStyle = new GUIStyle(FieldListValueLabelStyle);
					fieldListHeaderLabelStyle.normal.textColor = (fieldListHeaderLabelStyle.normal.textColor 
						+ Color.white) * 0.5f; // bias current color towards white, it gets multiplied by GUI.color
					fieldListHeaderLabelStyle.fontStyle = FontStyle.Bold;
				}
				return fieldListHeaderLabelStyle;
			}
		}
		
		public ObjectBrowserPanel(bool editor = false) : this(ObjectBrowser.instance, editor) {
		}
		
		public ObjectBrowserPanel(ObjectBrowser model, bool editor) {
			this.model = model;
			this.editor = editor;
			AddRootElement();
		}
		
		private float listItemHeight {
			get { return GUI.skin.button.lineHeight; }
		}

		public void DrawGui() {
			if (!editor) MaybeClearChildrenCache();
			DrawUpdateIntervalSlider(editor);
			DrawBreadcrumb();
			DrawFieldList();
			DoAction();
		}

		// Run this in EditorWindow.Update(). Returns true if should call to EditorWindow.Repaint() afterwards.
		public bool EditorWindowUpdate() {
			return MaybeClearChildrenCache();
		}
		
		private void AddRootElement() {
			var rootElem = Element.Create(model.GetRoot(), "Objects");
			rootElem.breadcrumbText = "Objects";
			path.Add(rootElem);
		}

		private void DrawUpdateIntervalSlider(bool editor) {
			GUILayout.BeginHorizontal();
			
			GUILayout.Label("Update interval: ");
			GUILayout.BeginVertical();
			if (!editor) GUILayout.Space(10f);
			childrenUpdateInterval = GUILayout.HorizontalSlider(childrenUpdateInterval, 0.01f, 1f, UpdateIntervalSliderLayout);
			GUILayout.EndVertical();
			GUILayout.Label(childrenUpdateInterval.ToString());
			GUILayout.FlexibleSpace();

			var labels = DisplayOptionUtils.Names;
			for (int i = 0; i < labels.Length; i++) {
				bool enabled = displayOptions.IsSet(i);
				enabled = GUILayout.Toggle(enabled, labels[i]);
				displayOptions = displayOptions.With(i, enabled);
			}
			
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
			var parentHandler = model.GetHandler(parent);
			var children = GetChildren(parent, parentHandler);
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
				var handler = model.GetHandler(obj);
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
			if (element.type == Element.Type.ValueRow) {
				GUI.enabled = !handler.IsLeaf(obj);
				if (GUILayout.Button(buttonText, FieldListButtonLayout)) {
					action = () => SelectChild(element);
				}
				GUI.enabled = true;
			}
			else if (element.type == Element.Type.Header) {
				var color = GUI.color;
				GUI.color = element.textColor;
				GUILayout.Label(element.text, FieldListHeaderLabelStyle);
				GUI.color = color;
			}
		}

		private void DrawBreadcrumb() {
			GUILayout.BeginHorizontal();
			using (var pathElements = path.GetEnumerator()) {
				Element parentElem = new Element();
				while (pathElements.MoveNext()) {
					var elem = pathElements.Current;
					var obj = elem.obj;
					if (elem.breadcrumbText == null) {
						elem.CreateBreadcrumbText(model.GetHandler(parentElem.obj), parentElem.obj, elem);
					}
					if (GUILayout.Button(elem.breadcrumbText, BreadcrumbButtonLayout)) {
						action = () => GoToAncestor(obj);
					}
					parentElem = elem;
				}
			}
			GUILayout.EndHorizontal();
		}

		private IList<Element> GetChildren(object parent, ITypeHandler parentHandler) {
			if (!childrenCached) {
				var enumerator = parentHandler.GetChildren(parent, displayOptions);
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

		private bool MaybeClearChildrenCache() {
			if (Time.realtimeSinceStartup >= childrenCacheTime + childrenUpdateInterval) {
				childrenCacheTime = Time.realtimeSinceStartup;
				ClearChildrenCache();
				return true;
			}
			return false;
		}
	}
}