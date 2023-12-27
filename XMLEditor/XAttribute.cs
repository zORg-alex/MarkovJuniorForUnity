using System;
using UnityEngine;

namespace zORgs.XML
{
	[Serializable]
	public class XAttribute
	{
		[SerializeField]
		private string _name;
		[SerializeField]
		private string _value;

		public XAttribute(string name, string value)
		{
			_name = name;
			_value = value;
		}

		public string Name { get => _name; set => _name = value; }
		public string Value { get => _value; set => _value = value; }
	}
}
