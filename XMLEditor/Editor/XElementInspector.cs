using RectEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace zORgs.XML
{
	[CustomPropertyDrawer(typeof(XElement))]
	public class XElementInspector : PropertyDrawer
	{
		public float LineHeight = EditorGUIUtility.singleLineHeight + 2;
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var val = (XElement)property.boxedValue;
			return val.Count() * (LineHeight + 2);
		}
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var val = (XElement)property.boxedValue;
			//base.OnGUI(position, property, label);
			var curLine = new Rect(position.x, position.y, position.width, LineHeight);

			foreach (var (xElement, intendation) in val.EnumarateIntendated())
			{
				var rect = curLine.CutFromLeft(intendation * 10)[1];
				EditorGUI.HelpBox(rect, string.Empty, MessageType.None);

				curLine = curLine.MoveDown();
			}
		}
	}
}