using System;
using System.Collections.Generic;
using UnityEngine;

namespace zORgs.Voxes
{
	public static class VoxesExtensions
	{
		public static T[] SelectArray<T, T2>(this T2[] array, Func<T2, T> select)
		{
			T[] ret = new T[array.Length];
			for ( var i = 0; i < array.Length; i++)
			{
				ret[i] = select(array[i]);
			}
			return ret;
		}
		public static List<T> SelectList<T,T2>(this IEnumerable<T2> collection, Func<T2,T> select)
		{
			var list = new List<T>();
			foreach ( var item in collection)
			{
				list.Add(select(item));
			}
			return list;
		}
		public static T[] SelectArray<T, T2>(this IEnumerable<T2> collection, Func<T2, T> select) =>
			collection.SelectList(select).ToArray();

		public static Color MultiplyAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);
		public static Color SetAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
		public static Color MultiplyColor(this Color c, float m) => new Color(c.r * m, c.g * m, c.b * m, c.a);

		public static Vox ToVox(this (byte[] result, Color[] pallete, int XLen, int YLen, int ZLen) tuple) =>
			new Vox(tuple.result.SelectArray(b => (int)b), new Vector3Int(tuple.XLen, tuple.YLen, tuple.ZLen), tuple.pallete);
	}
}