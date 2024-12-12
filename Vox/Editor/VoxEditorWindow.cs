using UnityEditor;
using UnityEngine;
using System.Reflection;
using BezierZUtility;
using UnityEngine.UIElements;

namespace zORgs.Voxes
{
	public class VoxEditorWindow : EditorWindow
	{
		[SerializeField]
		private SerializedProperty property;
		private Vox _localVox;
		private FieldInfo _fieldInfo;
		private Object _targetObject;
		internal static readonly Vector2 visualVoxelSize = new Vector2(32, 32);
		internal static readonly Vector2 visualVoxelOffset = new Vector2(16, 8);
		internal static readonly Vector3 normalizedOne = new Vector3(0.5773503f, 0.5773503f, 0.5773503f);
		internal static Texture2D _voxelTexture;
		internal static Texture2D _selectedVoxelTexture;
		internal static Texture2D _gridTexture;
		private int _layer;
		private Vector2 _mousePos;
		private bool _mouseDown;
		private int _mouseButton;
		private int _selectedColorId;
		private bool _mouseInside;

		[InitializeOnLoadMethod]
		private static void StaticInitialize()
		{
			_voxelTexture = Resources.Load<Texture2D>("vox");
			_gridTexture = Resources.Load<Texture2D>("vox grid");
			_selectedVoxelTexture = Resources.Load<Texture2D>("vox selected");
		}
		public static void ShowWindow(SerializedProperty property)
		{
			VoxEditorWindow window = GetWindow<VoxEditorWindow>();
			window.titleContent = new GUIContent("Vox Editor");
			window.Initialize(property);
			window.Show();
		}
		private void Initialize(SerializedProperty property) {
			this.property = property;
			_localVox = (Vox)property.boxedValue;
			if (_localVox.Size == Vector3Int.zero)
			{
				_localVox = Vox.Default;
				property.SetStruct(_localVox);
			}
			_selectedColorId = 1;
			_layer = _localVox.Size.y - 1;

			wantsMouseMove = true;
			wantsMouseEnterLeaveWindow = true;
		}

		private void OnGUI()
		{
			if (property == null)
			{
				EditorGUILayout.HelpBox("Nothing to edit", MessageType.Error);
				return;
			}
			var current = Event.current;
			var height = VisualVoxelHeight(_localVox);

			Vector3Int size = EditorGUILayout.Vector3IntField("Size", _localVox.Size);
			if (_localVox.Size != size)
			{
				Undo.RecordObject(property.serializedObject.targetObject, property.propertyPath + " Vox resized");
				_localVox.SetSize(size);
				property.SetStruct(_localVox);
			}

			var drawRect = GUILayoutUtility.GetRect(0, 0, GUILayout.Height(height));

			ReadInput(current, drawRect);

			var vOver = DrawVoxels(current, _localVox, drawRect, _layer, _mousePos, _mouseInside);

			if (_mouseDown && vOver.pos != default && (_mouseButton == 0 || _mouseButton == 1))
			{
				_localVox.SetVoxel(vOver.v.x, vOver.v.y, vOver.v.z, _mouseButton == 0 ? _selectedColorId : 0);
			}

			DrawPalette();
		}

		private void DrawPalette()
		{
			for (int i = 1; i < _localVox.PaletteLength; i++)
			{
				using (var horiz = new GUILayout.HorizontalScope())
				{
					var col = _localVox.GetColor(i);
					GUILayout.Label(i.ToString());
					var newCol = EditorGUILayout.ColorField(col);
					var selected = GUILayout.Toolbar(_selectedColorId == i ? 0 : -1, new[] { GUIContent.none }, GUILayout.Width(100));
					if (selected == 0)
					{
						_selectedColorId = i;
					}
					if (newCol != col)
					{
						_selectedColorId = i;
						_localVox.SetColor(newCol, i);
						Undo.RecordObject(property.serializedObject.targetObject, property.propertyPath + " Color Changed");
						property.SetStruct(_localVox);
					}
				}
			}
			if (GUILayout.Button("Remove Last"))
			{
				_localVox.RemoveLastColor();
				Undo.RecordObject(property.serializedObject.targetObject, property.propertyPath + " Color Removed");
				property.SetStruct(_localVox);
			}
			using (var horiz = new GUILayout.HorizontalScope())
			{
				GUILayout.Label(" ");
				var col = Color.red;
				var newCol = EditorGUILayout.ColorField(col);
				if (GUILayout.Button("Add", GUILayout.Width(100)) || newCol != col)
				{
					_selectedColorId = _localVox.PaletteLength;
					_localVox.AppendPallete(newCol);
					Undo.RecordObject(property.serializedObject.targetObject, property.propertyPath + " Color Added");
					property.SetStruct(_localVox);
					Repaint();
				}
			}
		}

		private void ReadInput(Event current, Rect drawRect)
		{
			if (current.type == EventType.MouseEnterWindow || current.type == EventType.MouseLeaveWindow)
				_mouseInside = current.type == EventType.MouseEnterWindow;
			if (current.isScrollWheel && CanScrollLayer(current, _layer, _localVox.Size.y - 1) && drawRect.Overlaps(new Rect(current.mousePosition, Vector2.zero)))
			{
				_layer += current.delta.y < 0 ? 1 : -1;
				_layer = Mathf.Clamp(_layer, 0, _localVox.Size.y - 1);
				Repaint();
				current.Use();
				return;
			}
			if (current.type == EventType.MouseDown || current.type == EventType.TouchDown)
			{
				_mouseDown = true;
				_mouseButton = current.button;
				Repaint();
			}
			if (current.type == EventType.MouseUp || current.type == EventType.TouchUp)
			{
				_mouseDown = false;
				Undo.RecordObject(property.serializedObject.targetObject, property.propertyPath + " voxels drawn");
				property.SetStruct(_localVox);
			}
			if (current.type == EventType.MouseMove || current.type == EventType.MouseDrag || current.type == EventType.TouchMove)
			{
				_mousePos = current.mousePosition;
				if (drawRect.Overlaps(new Rect(_mousePos, Vector2.zero)))
					Repaint();
			}
		}

		internal static bool IsOver(Vox.Voxel v, Rect rect, Vector2 mousepos, int layer)
		{
			var posInRect = mousepos - rect.position;
			if (v.y == layer &&
				rect.Overlaps(new Rect(mousepos, Vector2.zero)) &&
				_voxelTexture.GetPixel((int)posInRect.x, (int)posInRect.y).a > .5)
				return true;
			return false;
		}

		public static (Vox.Voxel v, Rect pos) DrawVoxels(Event current, Vox vox, Rect drawRect, int layer = -1,
			Vector2 mousePos = default, bool drawGrid = false)
		{

			var hOffset = VisualHorizontalOffset(vox);
			var center = drawRect.position + Vector2.right * (drawRect.width / 2 + hOffset) + Vector2.up * (vox.Size.y * 2) * visualVoxelOffset.y;
			if (current.type == EventType.Repaint)
			{
				(Vox.Voxel v, Rect pos) vOver = (default, default);
				foreach (var v in vox.GetVoxels())
				{
					var visible = layer >= v.y || layer == -1;
					float depth = .5f + VisualVoxelDepth(v) / vox.Size.magnitude / 2;
					Rect pos = new Rect(center + VoxelCanvasOffset(v) - visualVoxelSize / 2, visualVoxelSize);

					GUI.color = vox.GetColor(v.col).MultiplyColor(depth).MultiplyAlpha(visible ? 1 : .2f);
					GUI.DrawTexture(pos, _voxelTexture);
					GUI.color = Color.white;
					if (drawGrid && v.y == layer)
						GUI.DrawTexture(pos, _gridTexture);
					if (IsOver(v, pos, mousePos, layer))
						vOver = (v, pos);
				}
				if (vOver.pos != default)
				{
					GUI.DrawTexture(vOver.pos, _selectedVoxelTexture);
				}
				return vOver;
			}
			return default;
		}

		public static float VisualVoxelHeight(Vox vox) =>
			(vox.Size.y * 2 + vox.Size.x + vox.Size.z) * visualVoxelOffset.y;

		public static float VisualHorizontalOffset(Vox vox) =>
			(vox.Size.z - vox.Size.x) * visualVoxelOffset.x / 2;

		public static float VisualVoxelWidth(Vox vox) =>
			(vox.Size.x + vox.Size.z) * visualVoxelOffset.x;

		public static float VisualVoxelDepth((int x, int y, int z, int col) v)
		{
			return Vector3.Dot(new Vector3(v.x, v.y, v.z), normalizedOne);
		}

		public static Vector2 VoxelCanvasOffset((int x, int y, int z, int col) v)
		{
			return new Vector2(
				visualVoxelOffset.x * (v.x - v.z),
				visualVoxelOffset.y * (v.x - 2 * v.y + v.z)
				);
		}
		public static bool CanScrollLayer(Event current, int layer, int max) => 
			!((current.delta.y < 0 && layer == max) || (current.delta.y > 0 && layer == 0));
	}
}