using RectEx;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditorInternal;
using UnityEngine;
using Utility;

namespace zORgs.XML
{
	[CustomPropertyDrawer(typeof(XElement))]
	public class XElementInspector : PropertyDrawer
	{
		private static GUIStyle _attributeField;
		private Dictionary<string, ReorderableList> _lists = new Dictionary<string, ReorderableList>();
		public float LineHeight = EditorGUIUtility.singleLineHeight + 2;
		private SerializedObject _object;
		private Event _current;
		private static (string path, int ind) _editedAttributeInd;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			_lists.TryGetValue(property.propertyPath, out var list);
			if (list == null) return LineHeight;
			//return (list.serializedProperty.arraySize == 0 ? 0 : list.GetHeight()) + LineHeight;
			return list.GetHeight();
		}

		private (XElement, ReorderableList) Initialize(SerializedProperty property)
		{
			if (_object != property.serializedObject)
			{
				_object = property.serializedObject;
				_lists.Clear();
			}
			if (_attributeField == null)
			{
				_attributeField = "TextField";
				_attributeField.margin = new RectOffset(2, 2, 1, 1);
			}

			var val = property.GetValue<XElement>();
			SerializedProperty listProp = property.FindPropertyRelative("_children");
			if (!_lists.ContainsKey(listProp.propertyPath))
			{
				ReorderableList list = new ReorderableList(listProp.serializedObject, listProp);
				list.headerHeight = LineHeight;
				list.drawHeaderCallback += rect => DoHeader(_current, property.propertyPath, val, rect);
				list.drawElementCallback += (r, ind, isAct, isFoc) => ListDrawElement(list, r, ind, isAct, isFoc);
				list.drawElementBackgroundCallback += (r, ind, isAct, isFoc) => ListDrawElementBackground(list, r, ind, isAct, isFoc);
				list.elementHeightCallback += ind => ListElementHeight(list, ind);
				_lists[property.propertyPath] = list;
				return (val, list);
			}

			return (val, _lists[property.propertyPath]);
		}

		private float ListElementHeight(ReorderableList list, int index) =>
			list.serializedProperty.arraySize == 0 ? 0 :
			GetPropertyHeight(list.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none);

		private void ListDrawElement(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
		{
			if (index == 0 && list.serializedProperty.arraySize == 0)
				return;
			OnGUI(rect, list.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none);
		}

		private void ListDrawElementBackground(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
		{
			if (!isActive && !isFocused) return;
			GUI.color = isActive ? new Color(.7f, .7f, 1f) : new Color(.7f, .7f, 1f, .5f);
			GUI.Box(rect, GUIContent.none);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!property.propertyPath.Contains('.'))
				property.serializedObject.Update();

			_current = Event.current;
			if (_current.type == EventType.Layout) return;

			var (val, list) = Initialize(property);

			list.DoList(position);

			if (!property.propertyPath.Contains('.'))
				property.serializedObject.ApplyModifiedProperties();
		}

		private static void DoHeader(Event current, string propertyPath, XElement val, Rect firstLine)
		{
			var opening = new GUIContent("<");
			var closing = val.Children.Count > 0 ? new GUIContent(">") : new GUIContent("/>");
			var attrRects = firstLine.Row(val.Attributes.Count());
			var labels = new GUIContent[val.Attributes.Count];
			var widths = new float[val.Attributes.Count * 2];
			var rects = new Rect[val.Attributes.Count * 2 + 3];
			float openingWidth = EditorStyles.label.CalcSize(opening).x;
			rects[0] = firstLine.AlignLeft(openingWidth).Expand(0, -1);
			for (int i = 0; i < attrRects.Length; i++)
			{
				labels[i] = new GUIContent(val.Attributes[i].Name + "=");
				widths[i * 2] = EditorStyles.label.CalcSize(labels[i]).x;
				rects[i * 2 + 1] = rects[i * 2].MoveRightFor(widths[i * 2]);

				ProcessHeaderDoubleclick(current, propertyPath, rects[i * 2 + 1], i);
				if (_editedAttributeInd == (propertyPath, i))
				{
					widths[i * 2] = _attributeField.CalcSize(labels[i]).x;
					rects[i * 2 + 1].width = widths[i * 2];
				}

				widths[i * 2 + 1] = _attributeField.CalcSize(new GUIContent(val.Attributes[i].Value)).x;
				rects[i * 2 + 2] = rects[i * 2 + 1].MoveRightFor(widths[i * 2 + 1], 0);
			}
			rects[rects.Length - 2] = rects[rects.Length - 3].MoveRightFor(20);
			rects[rects.Length - 1] = rects[rects.Length - 2].MoveRightFor(EditorStyles.label.CalcSize(closing).x);

			EditorGUI.LabelField(rects[0], opening);
			for (int i = 0; i < attrRects.Length; i++)
			{
				Rect labelRect = rects[i * 2 + 1];
				if (_editedAttributeInd == (propertyPath, i))
					val.Attributes[i].Name = EditorGUI.TextField(labelRect, GUIContent.none, val.Attributes[i].Name, _attributeField);
				else
					EditorGUI.LabelField(labelRect, labels[i]);
				Rect textFieldRect = rects[i * 2 + 2];
				val.Attributes[i].Value = EditorGUI.TextField(textFieldRect, GUIContent.none, val.Attributes[i].Value, _attributeField);
			}
			if (GUI.Button(rects[rects.Length - 2], "+"))
				val.Attributes.Add(new XAttribute("attr", "0"));
			EditorGUI.LabelField(rects[rects.Length - 1], closing);

			int emptyIndex = val.Attributes.FindIndex(a => string.IsNullOrEmpty(a.Name));
			if (emptyIndex != -1 && _editedAttributeInd != (propertyPath, emptyIndex))
				val.Attributes.RemoveAt(emptyIndex);
		}

		private static void ProcessHeaderDoubleclick(Event current, string propertyPath, Rect rect, int i)
		{
			if (current.type == EventType.KeyDown &&
				(current.keyCode == KeyCode.Tab ||
				current.keyCode == KeyCode.KeypadEnter ||
				current.keyCode == KeyCode.Return))
			{
				_editedAttributeInd = default;
			}

			if (current.type == EventType.MouseDown)
			{
				if (rect.Contains(current.mousePosition))
				{
					if (current.clickCount == 2)
					{
						_editedAttributeInd = (propertyPath, i);
					}
					else
						_editedAttributeInd = default;
				}
			}
		}
	}
}