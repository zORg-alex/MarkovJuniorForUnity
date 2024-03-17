using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace zORgs.Voxes
{
	[Serializable]
	public struct Vox
	{
		public IEnumerable<Voxel> Voxels => GetVoxels();

		[SerializeField]
		private int[] _array;
		[SerializeField]
		private Vector3Int _size;
		public Vector3Int Size => _size;

		public static readonly Vox Default = new Vox(new[] { 1 }, Vector3Int.one, new[] { Color.clear, Color.white });

		[SerializeField]
		private Color[] _palette;
		public int PaletteLength => _palette.Length;

		public bool IsDefault => _size == default;

		public Vox(int[] array)
		{
			if (array is null) _array = new int[] { 1, 1, 1, 1 };
			_size = new Vector3Int(array[0], array[1], array[2]);
			_array = new Span<int>(array, 3, array.Length - 3).ToArray();
			var pallete = new List<Color>() { Color.clear };
			int count = _array.Distinct().Count();
			pallete.AddRange(Enumerable.Repeat(Color.white, count - 1));
			_palette = pallete.ToArray();
		}

		public Vox(int[] array, Vector3Int size, Color[] palette)
		{
			_array = array;
			_size = size;
			_palette = palette;
		}

		public void SetPallete(Color[] palete) => _palette = palete;
		public void SetColor(Color col, int index)
		{
			_palette[index] = col;
		}
		public void RemoveLastColor()
		{
			if (PaletteLength <= 1) return;

			_palette = new Span<Color>(_palette, 0, _palette.Length - 1).ToArray();
			for (int i = 0; i < _array.Length; i++)
			{
				if (_array[i] == _palette.Length)
					_array[i] = 0;
			}
		}
		public Color GetColor(int index)
		{
			if (index >= _palette.Length) return Color.magenta;
			return _palette[index];
		}
		public void SetSize(Vector3Int size)
		{
			if (size.x <= 0 || size.y <= 0 || size.z <= 0) return;

			var array = new int[size.x * size.y * size.z];
			var sx = Mathf.Min(_size.x, size.x);
			var sy = Mathf.Min(_size.y, size.y);
			var sz = Mathf.Min(_size.z, size.z);
			for (int z = 0; z < sz; z++)
			{
				var offsetZ = z * _size.x * _size.y;
				var newOffsetZ = z * size.x * size.y;
				for (int y = 0; y < sy; y++)
				{
					var offsetY = y * _size.x;
					var newOffsetY = y * size.x;
					for (int x = 0; x < sx; x++)
					{
						array[x + newOffsetY + newOffsetZ] = _array[x + offsetY + offsetZ];
					}
				}
			}
			_array = array;
			_size = size;
		}

		public static implicit operator Vox(int[] array) => new Vox(array);

		public IEnumerable<Voxel> GetVoxels(bool skipEmpty = false)
		{
			for (int z = 0; z < _size.z; z++)
			{
				var offsetZ = z * _size.x * _size.y;
				for (int y = 0; y < _size.y; y++)
				{
					var offsetY = y * _size.x;
					for (int x = 0; x < _size.x; x++)
					{
						int col = _array[x + offsetY + offsetZ];
						if (col == 0 && skipEmpty) continue;
						yield return new Voxel(x, y, z, col);
					}
				}
			}
		}

		public void SetVoxel(int x, int y, int z, int col)
		{
			if (col >= _palette.Length || x < 0 || x >= _size.x || y < 0 || y >= _size.y || z < 0 || z >= _size.z) return;

			_array[x + y * _size.x + z * _size.x * _size.y] = col;
		}

		internal void AppendPallete(Color newCol)
		{
			_palette = _palette.Append(newCol).ToArray();
		}

		public (int[], int, int, int) ToInts() => (_array, _size.x, _size.y, _size.z);

		public struct Voxel
		{
			public int x;
			public int y;
			public int z;
			public int col;

			public Voxel(int x, int y, int z, int col)
			{
				this.x = x;
				this.y = y;
				this.z = z;
				this.col = col;
			}

			public override bool Equals(object obj)
			{
				return obj is Voxel other &&
					   x == other.x &&
					   y == other.y &&
					   z == other.z &&
					   col == other.col;
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(x, y, z, col);
			}

			public void Deconstruct(out int x, out int y, out int z, out int col)
			{
				x = this.x;
				y = this.y;
				z = this.z;
				col = this.col;
			}

			public static implicit operator (int x, int y, int z, int col)(Voxel value)
			{
				return (value.x, value.y, value.z, value.col);
			}

			public static implicit operator Voxel((int x, int y, int z, int col) value)
			{
				return new Voxel(value.x, value.y, value.z, value.col);
			}
		}
	}

}