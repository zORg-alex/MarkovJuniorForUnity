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
using UnityEngine.UIElements;
using Utility;
using zORgs.Voxes;

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

		private (XElement, ReorderableList) Initialize(Rect position, SerializedProperty property)
		{
			ReorderableList.defaultBehaviours.headerBackground.fixedHeight = 0;
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
			SerializedProperty attributesProp = property.FindPropertyRelative("_attributes");
			if (!_lists.ContainsKey(property.propertyPath))
			{
				ReorderableList list = new ReorderableList(listProp.serializedObject, listProp);
				_lists[property.propertyPath] = list;
				list.headerHeight = DoHeader(null, property.propertyPath, attributesProp, val, position);
				list.drawHeaderCallback += rect => DoHeader(_current, property.propertyPath, attributesProp, val, rect);
				list.drawElementCallback += (r, ind, isAct, isFoc) => DoListElement(list, r, ind, isAct, isFoc);
				list.drawNoneElementCallback += rect => { };
				list.drawElementBackgroundCallback += (r, ind, isAct, isFoc) => DoListElementBackground(list, r, ind, isAct, isFoc);
				list.elementHeightCallback += ind => ListElementHeight(list, ind);
				return (val, list);
			}

			return (val, _lists[property.propertyPath]);
		}

		private float ListElementHeight(ReorderableList list, int index) =>
			list.serializedProperty.arraySize == 0 ? 0 :
			GetPropertyHeight(list.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none);

		private void DoListElement(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
		{
			if (index == 0 && list.serializedProperty.arraySize == 0)
				return;
			OnGUI(rect, list.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none);
		}

		private void DoListElementBackground(ReorderableList list, Rect rect, int index, bool isActive, bool isFocused)
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

			var (val, list) = Initialize(position, property);

			list.DoList(position);

			if (!property.propertyPath.Contains('.'))
				property.serializedObject.ApplyModifiedProperties();
		}

		private float DoHeader(Event current, string propertyPath, SerializedProperty property, XElement val, Rect firstLine)
		{
			var currentLine = firstLine.AlignTop(LineHeight).SetY(firstLine.y + 2);
			var opening = new GUIContent("<"+val.Name);
			var closing = val.Children.Count > 0 ? new GUIContent(">") : new GUIContent("/>");
			int attrCount = val.Attributes.Count;
			var labels = new GUIContent[attrCount];
			var rects = new Rect[attrCount * 2 + 3];
			float openingWidth = EditorStyles.label.CalcSize(opening).x;
			int lineCount = 1;
			int rectInd = 0;
			rects[0] = currentLine.AlignLeft(openingWidth).Padding(0, -1);
			ProcessHeaderDoubleclick(current, propertyPath, rects[0], -1);
			float lineWidth = firstLine.width - rects[0].width;
			bool isFirst = true;
			for (int i = 0; i < attrCount; i++)
			{
				labels[i] = new GUIContent(val.Attributes[i].Name + "=");
				float labelWidth = EditorStyles.label.CalcSize(labels[i]).x;

				if (_editedAttributeInd == (propertyPath, i))
					labelWidth = _attributeField.CalcSize(labels[i]).x;

				float attrWidth = val.Attributes[i].IsVox ? VoxEditorWindow.VisualVoxelWidth(val.Attributes[i].Vox) :
					_attributeField.CalcSize(new GUIContent(val.Attributes[i].Value)).x;

				lineWidth -= labelWidth + attrWidth + 2;
				//Label rect with linewrap
				if (i == attrCount - 1)
					lineWidth -= 30;
				if (!isFirst && (lineWidth < 0))
				{
					lineWidth = firstLine.width - (labelWidth + attrWidth + 2);
					lineCount++;
					currentLine = currentLine.MoveDownFor(LineHeight + 2);
					rects[++rectInd] = currentLine.AlignLeft(labelWidth).SetHeight(LineHeight);
				}
				else
					rects[++rectInd] = rects[rectInd - 1].MoveRightFor(labelWidth).SetHeight(LineHeight);

				ProcessHeaderDoubleclick(current, propertyPath, rects[rectInd], i);

				//value rect
				if (val.Attributes[i].IsVox)
				{
					float voxHeight = VoxEditorWindow.VisualVoxelHeight(val.Attributes[i].Vox);
					if (currentLine.height < voxHeight)
						currentLine = currentLine.SetHeight(voxHeight);
					rects[++rectInd] = rects[rectInd - 1].MoveRightFor(attrWidth, 0).SetHeight(currentLine.height);
				}
				else
					rects[++rectInd] = rects[rectInd - 1].MoveRightFor(attrWidth, 0).SetHeight(LineHeight);

				//Context menu for Voxels and conversion
				if (Event.current.type == EventType.ContextClick && rects[rectInd].Contains(Event.current.mousePosition))
				{
					GenericMenu menu = new GenericMenu();
					var attr = val.Attributes[i];
					if (attr.IsVox)
					{
						var index = i;
						menu.AddItem(new GUIContent("Edit"), false, () =>
						{
							SerializedProperty voxProp = property.GetArrayElementAtIndex(index).FindPropertyRelative("_vox");
							VoxEditorWindow.ShowWindow(voxProp);
						});
						menu.AddItem(new GUIContent("Convert To Text"), false, () => attr.Value = "0");
					}
					else
						menu.AddItem(new GUIContent("Convert to Voxel"), false, () => attr.Vox = new Vox(new int[] { 1, 1, 1, 1 }));
					menu.ShowAsContext();
					Event.current.Use();
				}

				isFirst = false;
			}
			rects[++rectInd] = rects[rectInd - 1].MoveRightFor(20).SetHeight(LineHeight);
			rects[++rectInd] = rects[rectInd - 1].MoveRightFor(EditorStyles.label.CalcSize(closing).x).SetHeight(LineHeight);

			var listHeaderHeight = currentLine.yMax - firstLine.yMin + 4;
			var list = _lists[propertyPath];
			if (list.headerHeight != listHeaderHeight)
				list.headerHeight = listHeaderHeight;

			if (current == null)
				return listHeaderHeight;

			rectInd = 0;

			if (_editedAttributeInd == (propertyPath, -1))
				val.Name = EditorGUI.TextField(rects[rectInd++], val.Name);
			else
				EditorGUI.LabelField(rects[rectInd++], opening);

			for (int i = 0; i < attrCount; i++)
			{
				if (_editedAttributeInd == (propertyPath, i))
					val.Attributes[i].Name = EditorGUI.TextField(rects[rectInd++], GUIContent.none, val.Attributes[i].Name, _attributeField);
				else
					EditorGUI.LabelField(rects[rectInd++], labels[i]);

				if (val.Attributes[i].IsVox)
				{
					VoxEditorWindow.DrawVoxels(current, val.Attributes[i].Vox, rects[rectInd++], 1000);
				}
				else
				{
					var value = EditorGUI.TextField(rects[rectInd++], GUIContent.none, val.Attributes[i].Value, _attributeField);
					if (val.Attributes[i].Value != value)
						val.Attributes[i].Value = value;
				}
			}
			if (GUI.Button(rects[rectInd++], "+"))
				val.Attributes.Add(new XAttribute("attr", "0"));
			EditorGUI.LabelField(rects[rectInd++], closing);

			int emptyIndex = val.Attributes.FindIndex(a => string.IsNullOrEmpty(a.Name));
			if (emptyIndex != -1 && _editedAttributeInd != (propertyPath, emptyIndex))
				val.Attributes.RemoveAt(emptyIndex);

			return listHeaderHeight;
		}

		private static void ProcessHeaderDoubleclick(Event current, string propertyPath, Rect rect, int i)
		{
			if (current == null) return;

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