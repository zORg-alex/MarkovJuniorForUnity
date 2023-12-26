using System;
using UnityEditor;
using UnityEngine;
using Utility;

namespace zORgs.Voxes
{

	[CustomPropertyDrawer(typeof(Vox))]
	public class VoxInspector : PropertyDrawer
	{
		private bool _initialized;
		private Event current;
		private int _layer;
		private static Vector2 visualVoxelSize => VoxEditorWindow.visualVoxelSize;
		private static Vector2 visualVoxelOffset => VoxEditorWindow.visualVoxelOffset;
		private static Vector3 normalizedOne => VoxEditorWindow.normalizedOne;
		private static Texture2D _voxelTexture => VoxEditorWindow._voxelTexture;

		internal static float VisualVoxelHeight(Vox vox) => VoxEditorWindow.VisualVoxelHeight(vox);

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
			VisualVoxelHeight((Vox)property.boxedValue) + EditorGUIUtility.singleLineHeight * 2;

		private void Initialize(Vox vox, SerializedProperty property)
		{
			if (vox.Size == Vector3Int.zero)
			{
				vox = Vox.Default;
				property.SetStruct(vox);
			}

			_layer = vox.Size.y;
			_initialized = true;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var vox = (Vox)property.boxedValue;
			if (!_initialized) Initialize(vox, property);
			var c = GUI.color;
			current = Event.current;

			if (current.type == EventType.Layout) return;

			Rect labelRect = new(position.position, new Vector2(position.width, EditorGUIUtility.singleLineHeight));
			EditorGUI.LabelField(labelRect, label);

			using (var props = new EditorGUI.PropertyScope(position, label, property))
			using (var change = new EditorGUI.ChangeCheckScope())
			{
				Rect voxelRect = new(labelRect.position + Vector2.up * labelRect.height, new Vector2(position.width, VisualVoxelHeight(vox)));
				Rect editRect = new(voxelRect.position + Vector2.up * voxelRect.height, new Vector2(position.width, EditorGUIUtility.singleLineHeight));
				GUI.Box(voxelRect, GUIContent.none, EditorStyles.helpBox);

				if (GUI.Button(editRect, "Edit"))
					VoxEditorWindow.ShowWindow(property);

				if (current.isScrollWheel && VoxEditorWindow.CanScrollLayer(current, _layer, vox.Size.y - 1) && voxelRect.Overlaps(new Rect(current.mousePosition, Vector2.zero)))
				{
					_layer += current.delta.y < 0 ? 1 : -1;
					_layer = Mathf.Clamp(_layer, 0, vox.Size.y - 1);
					RepaintInspector(property.serializedObject);
					current.Use();
				}

				VoxEditorWindow.DrawVoxels(current, vox, voxelRect, _layer);

				if (change.changed)
				{

				}
			}

			GUI.color = c;
		}

		internal static float VisualVoxelDepth((int x, int y, int z, int col) v) => VoxEditorWindow.VisualVoxelDepth(v);

		internal static Vector2 VoxelCanvasOffset((int x, int y, int z, int col) v) => VoxEditorWindow.VoxelCanvasOffset(v);

		public static void RepaintInspector(SerializedObject serializedObject)
		{
			EditorUtility.SetDirty(serializedObject.targetObject);
		}
	}
}