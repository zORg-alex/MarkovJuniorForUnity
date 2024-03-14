using System;
using UnityEngine;
using zORgs.Voxes;

namespace zORgs.XML
{
	[Serializable]
	public class XAttribute
	{
		[SerializeField]
		private string _name;
		[SerializeField]
		private string _value;
		[SerializeField]
		private Vox _vox;

		public XAttribute(string name, string value)
		{
			_name = name;
			_value = value;
		}

		public string Name { get => _name; set => _name = value; }
		public string Value { get => _value; set => _value = value; }
		public Vox Vox { get => _vox; set => _vox = value; }
		public bool IsVox => !_vox.IsDefault;
	}
}
