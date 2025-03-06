using UnityEngine;

namespace EditorTweaks
{
	public class CoordinateText : MonoBehaviour
	{
		public static string Text = "";
		public static bool Show = false;
		public static CoordinateText Instance;

		void OnGUI()
		{
			if (!Show) return;
			if (!ADOBase.isLevelEditor) return;

			GUIStyle guiShadow = new GUIStyle();
			guiShadow.fontSize = 40;
			guiShadow.font = RDString.GetFontDataForLanguage(RDString.language).font;
			guiShadow.normal.textColor = Color.black.WithAlpha(0.5f);
			GUI.Label(new Rect(2, 2, Screen.width, Screen.height), Text, guiShadow);

			GUIStyle gui = new GUIStyle();
			gui.fontSize = 40;
			gui.font = RDString.GetFontDataForLanguage(RDString.language).font;
			gui.normal.textColor = Color.white;
			GUI.Label(new Rect(0, 0, Screen.width, Screen.height), Text, gui);
		}
	}
}
