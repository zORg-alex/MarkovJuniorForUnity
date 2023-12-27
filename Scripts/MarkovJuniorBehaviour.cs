using System.Collections;
using System.Linq;
using UnityEngine;
using zORgs.XML;

namespace Assets.Plugins.MarkovJuniorForUnity.Scripts
{
	public class MarkovJuniorBehaviour : MonoBehaviour
	{
		public XElement Root;
		public void Run()
		{

		}

		private void Reset()
		{
			int i = 0;
			Root = new XElement();
			Root.Add(new XElement());
			Root.Add(new XElement());
			foreach (var child in Root.ToList())
				child.Add(new XElement());
			foreach (var child in Root)
				child["attr"+i] = i.ToString();
		}
	}
#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(MarkovJuniorBehaviour))]
	public class MarkovJuniorBehaviourInspector : UnityEditor.Editor
    {
		public override void OnInspectorGUI()
		{
			var c = UnityEngine.GUI.color;
			UnityEngine.GUI.color = new Color(.7f,1f,7f);
			if (UnityEngine.GUILayout.Button("Run"))
			{
				((MarkovJuniorBehaviour)target).Run();
			}
			UnityEngine.GUI.color = c;
			base.OnInspectorGUI();
		}
	}
#endif
}