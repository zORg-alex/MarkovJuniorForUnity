using RectEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Utility;

namespace zORgs.XML
{
	[CustomPropertyDrawer(typeof(XElement))]
	public class XElementInspector : PropertyDrawer
	{
		private Dictionary<string, ReorderableList> _lists = new Dictionary<string, ReorderableList>();
		public float LineHeight = EditorGUIUtility.singleLineHeight + 2;
		private SerializedObject _object;
		private Event _current;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			_lists.TryGetValue(property.propertyPath, out var list);
			return (list?.GetHeight() ?? 0) + LineHeight;
		}

		private (XElement, ReorderableList) Initialize(SerializedProperty property)
		{
			if (_object != property.serializedObject)
			{
				_object = property.serializedObject;
				_lists.Clear();
			}

			var val = property.GetValue<XElement>();
			SerializedProperty listProp = property.FindPropertyRelative("_children");
			if (!_lists.ContainsKey(listProp.propertyPath))
			{
				ReorderableList list = new ReorderableList(listProp.serializedObject, listProp, true, false, true, true);
				list.onCanAddCallback += l => true;
				list.onCanRemoveCallback += l => true;
				list.onAddCallback += ListAdd;
				list.onRemoveCallback += ListRemove;
				list.onSelectCallback += ListSelect;
				list.drawElementCallback += (r, ind, isAct, isFoc) => ListDrawElement(list, r, ind, isAct, isFoc);
				list.elementHeightCallback += ind => ListElementHeight(list, ind);
				_lists[property.propertyPath] = list;
				return (val, list);
			}

			return (val, _lists[property.propertyPath]);
		}

		private float ListElementHeight(ReorderableList list, int index) =>
			GetPropertyHeight(list.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none);

		private void ListDrawElement(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
		{
			OnGUI(rect, list.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none);
		}

		private void ListSelect(ReorderableList list)
		{

		}

		private void ListRemove(ReorderableList list)
		{
			var l = list.serializedProperty.GetValue<List<XElement>>();
			l.RemoveAt(list.index);
		}

		private void ListAdd(ReorderableList list)
		{
			var l = list.serializedProperty.GetValue<List<XElement>>();
			l.Add(new XElement());
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			_current = Event.current;
			if (_current.type == EventType.Layout) return;

			var (val, list) = Initialize(property);

			var rects = position.CutFromTop(LineHeight);
			var curLine = rects[0];

			EditorGUI.HelpBox(curLine, string.Empty, MessageType.None);
			var attrRects = curLine.Row(val.Attributes.Count());
			for (int i = 0; i < attrRects.Length; i++)
			{
				val.Attributes[i].Value = EditorGUI.TextField(attrRects[i].Extend(-2), val.Attributes[i].Name, val.Attributes[i].Value);
			}

			list.DoList(rects[1]);
		}
	}
}