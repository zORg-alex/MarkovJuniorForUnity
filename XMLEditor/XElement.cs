using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace zORgs.XML
{
	[Serializable]
	public class XElement : IEnumerable<XElement>
	{
		private const int MAX_DEPTH = 10;

		[SerializeField]
		private string _body;
		[SerializeField]
		private List<XElement> _children = new List<XElement>();
		[SerializeField]
		private List<XAttribute> _attributes = new List<XAttribute>();
		[SerializeField]
		private bool _hasParent;

		public string Body { get => _body; set => _body = value; }
		public List<XElement> Children => _children;
		public bool HasChildren => _children.Count > 0;
		public List<XAttribute> Attributes => _attributes;
		public bool HasParent => _hasParent;

		public string this[string name]
		{
			get => _attributes.FirstOrDefault()?.Value ?? string.Empty;
			set
			{
				var ind = _attributes.FindIndex(a => a.Name == name);
				if (ind == -1)
					_attributes.Add(new XAttribute(name, value));
				else
					_attributes[ind].Value = value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<XElement> GetEnumerator()
		{
			foreach ((XElement element, int indentation) in EnumarateIntendated())
			{
				yield return element;
			}
		}

		public IEnumerable<(XElement element, int indentation)> EnumarateIntendated() => EnumarateIntendated(0);
		private IEnumerable<(XElement element, int indentation)> EnumarateIntendated(int i)
		{
			yield return (this, i);
			if (i == MAX_DEPTH) yield break;
			foreach (var elem in _children)
			{
				foreach (var elementContext in elem.EnumarateIntendated(i + 1))
				{
					yield return elementContext;
				}
			}
		}

		public void Add(XElement element)
		{
			element._hasParent = true;
			_children.Add(element);
		}

		public void Remove(XElement element) => _children.Remove(element);
		public void RemoveAt(int index) => _children.RemoveAt(index);
	}
}
