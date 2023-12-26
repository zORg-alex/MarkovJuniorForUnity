
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Utility {
	/// <summary>
	/// https://gist.github.com/douduck08/6d3e323b538a741466de00c30aa4b61f
	/// </summary>
	public static class InspectorSeriallizedUtils {

		public static T GetValue<T>(this SerializedObject serializedObject, string path)
		{
			try
			{
				if (serializedObject.targetObject == null) return default;
			}
			catch
			{
				return default;
			}
			object obj = serializedObject.targetObject;
			path = path.Replace(".Array.data", "");
			string[] fieldStructure = path.Split('.');
			Regex rgx = new Regex(@"\[\d+\]");
			for (int i = 0; i < fieldStructure.Length; i++)
			{
				if (fieldStructure[i].Contains("["))
				{
					int index = System.Convert.ToInt32(new string(fieldStructure[i].Where(c => char.IsDigit(c)).ToArray()));
					obj = GetFieldValueWithIndex(rgx.Replace(fieldStructure[i], ""), obj, index);
				}
				else
				{
					obj = GetFieldValue(fieldStructure[i], obj);
				}
			}
			return (T)obj;
		}

		public static T GetValue<T>(this SerializedProperty property) where T : class
		{
			try
			{
				if (property.serializedObject.targetObject == null) return null;
				if (property.propertyPath == null) return null;
			}
			catch
			{
				return null;
			}
			object obj = property.serializedObject.targetObject;
			string path = property.propertyPath.Replace(".Array.data", "");
			string[] fieldStructure = path.Split('.');
			Regex rgx = new Regex(@"\[\d+\]");
			for (int i = 0; i < fieldStructure.Length; i++)
			{
				if (fieldStructure[i].Contains("["))
				{
					int index = System.Convert.ToInt32(new string(fieldStructure[i].Where(c => char.IsDigit(c)).ToArray()));
					obj = GetFieldValueWithIndex(rgx.Replace(fieldStructure[i], ""), obj, index);
				}
				else
				{
					obj = GetFieldValue(fieldStructure[i], obj);
				}
			}
			return (T)obj;
		}

		public static bool SetValue<T>(this SerializedProperty property, T value) where T : class
		{
			object obj = property.serializedObject.targetObject;
			string path = property.propertyPath.Replace(".Array.data", "");
			string[] fieldStructure = path.Split('.');
			Regex rgx = new Regex(@"\[\d+\]");
			for (int i = 0; i < fieldStructure.Length - 1; i++)
			{
				if (fieldStructure[i].Contains("["))
				{
					int index = System.Convert.ToInt32(new string(fieldStructure[i].Where(c => char.IsDigit(c)).ToArray()));
					obj = GetFieldValueWithIndex(rgx.Replace(fieldStructure[i], ""), obj, index);
				}
				else
				{
					obj = GetFieldValue(fieldStructure[i], obj);
				}
			}

			string fieldName = fieldStructure.Last();
			if (fieldName.Contains("["))
			{
				int index = System.Convert.ToInt32(new string(fieldName.Where(c => char.IsDigit(c)).ToArray()));
				return SetMemberValueWithIndex(rgx.Replace(fieldName, ""), obj, index, value);
			}
			else
			{
				return SetMemberValue(fieldName, obj, value);
			}
		}
		public static bool SetStruct<T>(this SerializedProperty property, T value) where T : struct
		{
			object obj = property.serializedObject.targetObject;
			string path = property.propertyPath.Replace(".Array.data", "");
			string[] fieldStructure = path.Split('.');
			Regex rgx = new Regex(@"\[\d+\]");
			for (int i = 0; i < fieldStructure.Length - 1; i++)
			{
				if (fieldStructure[i].Contains("["))
				{
					int index = System.Convert.ToInt32(new string(fieldStructure[i].Where(c => char.IsDigit(c)).ToArray()));
					obj = GetFieldValueWithIndex(rgx.Replace(fieldStructure[i], ""), obj, index);
				}
				else
				{
					obj = GetFieldValue(fieldStructure[i], obj);
				}
			}

			string fieldName = fieldStructure.Last();
			if (fieldName.Contains("["))
			{
				int index = System.Convert.ToInt32(new string(fieldName.Where(c => char.IsDigit(c)).ToArray()));
				return SetMemberValueWithIndex(rgx.Replace(fieldName, ""), obj, index, value);
			}
			else
			{
				return SetMemberValue(fieldName, obj, value);
			}
		}

		private static object GetFieldValue(string fieldName, object obj, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) {
			FieldInfo field = obj.GetType().GetField(fieldName, bindings);
			if (field != null) {
				return field.GetValue(obj);
			}
			var pinfo = obj.GetType().GetProperty(fieldName, bindings);
			if (pinfo != null)
			{
				return pinfo.GetValue(obj);
			}
			return default(object);
		}

		private static object GetFieldValueWithIndex(string fieldName, object obj, int index, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) {
			FieldInfo field = obj.GetType().GetField(fieldName, bindings);
			if (field != null) {
				object val = field.GetValue(obj);
				if (val.GetType().IsArray && val is object[] arr && index < arr.Length) {
					return arr[index];
				} else if (val is IEnumerable && val is IList lst && index < lst.Count) {
					return lst[index];
				}
			}
			var pinfo = obj.GetType().GetProperty(fieldName, bindings);
			if (pinfo != null)
			{
				object val = pinfo.GetValue(obj);
				if (val.GetType().IsArray && val is object[] arr && index < arr.Length)
				{
					return arr[index];
				} else if (val is IEnumerable && val is IList lst && index < lst.Count)
				{
					return lst[index];
				}
			}
			return default(object);
		}

		public static bool SetMemberValue(string fieldName, object obj, object value, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) {
			FieldInfo field = obj.GetType().GetField(fieldName, bindings);
			if (field != null) {
				field.SetValue(obj, value);
				return true;
			}
			PropertyInfo pinfo = obj.GetType().GetProperty(fieldName, bindings);
			if (pinfo != null)
			{
				pinfo.SetValue(obj, value);
				return true;
			}
			return false;
		}

		public static bool SetMemberValueWithIndex(string fieldName, object obj, int index, object value, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) {
			FieldInfo field = obj.GetType().GetField(fieldName, bindings);
			if (field != null) {
				object list = field.GetValue(obj);
				if (list.GetType().IsArray) {
					((Array)list).SetValue(value, index);
					//((object[])list)[index] = value;
					return true;
				} else if (value is IEnumerable) {
					((IList)list)[index] = value;
					return true;
				}
			}
			PropertyInfo pinfo = obj.GetType().GetProperty(fieldName, bindings);
			if (pinfo != null)
			{
				object list = pinfo.GetValue(obj);
				if (list.GetType().IsArray)
				{
					((object[])list)[index] = value;
					return true;
				} else if (value is IEnumerable)
				{
					((IList)list)[index] = value;
					return true;
				}
			}
			return false;
		}
	}
}