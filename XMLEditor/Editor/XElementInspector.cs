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
		private Dictionary<string, ReorderableList> _lists = new Dictionary<string, ReorderableList>();
		public float LineHeight = EditorGUIUtility.singleLineHeight + 2;
		private SerializedObject _object;
		private Event _current;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			_lists.TryGetValue(property.propertyPath, out var list);
			if (list == null) return LineHeight;
			return (list.serializedProperty.arraySize == 0 ? 0 : list.GetHeight()) + LineHeight;
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
			list.serializedProperty.arraySize == 0 ? 0 :
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

			var cutPosition = position.CutFromTop(LineHeight);
			var firstLine = cutPosition[0];

			EditorGUI.HelpBox(firstLine, string.Empty, MessageType.None);

			var opening = new GUIContent("<");
			var closing = val.Children.Count > 0 ? new GUIContent(">") : new GUIContent("/>");
			var attrRects = firstLine.Row(val.Attributes.Count());
			var labels = new GUIContent[val.Attributes.Count];
			var widths = new float[val.Attributes.Count * 2];
			var rects = new Rect[val.Attributes.Count * 2 + 2];
			float openingWidth = EditorStyles.label.CalcSize(opening).x;
			rects[0] = firstLine.AlignLeft(openingWidth).Expand(0, -2);
			for (int i = 0; i < attrRects.Length; i++)
			{
				labels[i] = new GUIContent(" " + val.Attributes[i].Name + " = ");
				widths[i * 2] = EditorStyles.label.CalcSize(labels[i]).x;
				widths[i * 2 + 1] = EditorStyles.textField.CalcSize(new GUIContent(val.Attributes[i].Value)).x;
				rects[i * 2 + 1] = rects[i * 2].MoveRightFor(widths[i * 2], 0);
				rects[i * 2 + 2] = rects[i * 2 + 1].MoveRightFor(widths[i * 2 + 1], 0);
			}
			rects[rects.Length - 1] = rects[rects.Length - 2].MoveRightFor(EditorStyles.label.CalcSize(closing).x);

			EditorGUI.LabelField(rects[0], opening);
			for (int i = 0; i < attrRects.Length; i++)
			{
				Rect labelRect = rects[i * 2 + 1];
				EditorGUI.LabelField(labelRect, labels[i]);
				Rect textFieldRect = rects[i * 2 + 2];
				val.Attributes[i].Value = EditorGUI.TextField(textFieldRect, GUIContent.none, val.Attributes[i].Value);
			}
			EditorGUI.LabelField(rects[rects.Length - 1], closing);

			if(val.Children.Count > 0)
				list.DoList(cutPosition[1]);
		}
	}
}