using System;
using UnityEngine;
using UnityModManagerNet;

namespace EditorTweaks.Utils
{
	public static class GUILayoutUtils
	{
		public static bool IsHover()
		{
			if (!Input.GetKey(KeyCode.Mouse0) && !Input.GetKey(KeyCode.Mouse1))
			{
				return Event.current.type == EventType.Repaint
					&& GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
			}

			return false;
		}

		public static void DrawTooltip(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				GUI.Box(new Rect(0, 0, 0, 0), "");
			}
			else
			{
				Vector2 mousepos = Event.current.mousePosition;
				Rect strpos = new Rect(mousepos.x, mousepos.y, 0, 0);
				Vector2 strsize = GUI.skin.label.CalcSize(new GUIContent(str));

				strpos.y -= strsize.y + 20;

				strpos.width = strsize.x + 20;
				strpos.height = strsize.y + 20;
				GUI.Box(strpos, "");
				GUI.Box(strpos, "");

				strpos.x += 10;
				strpos.y += 10;
				GUI.Label(strpos, str);
			}
		}

		public static bool HorizontalSlider(ref float value, float leftValue, float rightValue, params GUILayoutOption[] options)
		{
			float orig = value;
			value = GUILayout.HorizontalSlider(value, leftValue, rightValue, options);
			return orig != value;
		}

		public static void DrawFloatWithSlider(ref float value, float leftValue, float rightValue, string label, bool clamp = true, GUILayoutOption[] sliderOptions = null, GUILayoutOption[] fieldOptions = null)
		{
			GUILayout.BeginHorizontal();

			if (fieldOptions == null) fieldOptions = new GUILayoutOption[] { GUILayout.Width(75) };
			if (sliderOptions == null) sliderOptions = new GUILayoutOption[] { GUILayout.Width(200) };
			UnityModManager.UI.DrawFloatField(ref value, label, null, fieldOptions);
			GUILayout.Space(5);
			if (HorizontalSlider(ref value, leftValue, rightValue, sliderOptions))
				value = (float)Math.Round(value, 2);
			if (clamp) value = Mathf.Clamp(value, leftValue, rightValue);

			GUILayout.EndHorizontal();
		}

		public static bool DrawFloatWithTooltip(ref float value, float minValue, float maxValue, string label, bool clamp = true, bool tooltip = true, GUILayoutOption[] sliderOptions = null, GUILayoutOption[] fieldOptions = null)
		{
			GUILayout.BeginHorizontal();

			if (fieldOptions == null) fieldOptions = new GUILayoutOption[] { GUILayout.Width(75) };
			UnityModManager.UI.DrawFloatField(ref value, label, null, fieldOptions);
			if (clamp) value = Mathf.Clamp(value, minValue, maxValue);
			GUILayout.Space(5);

			bool result = false;
			if (tooltip)
			{
				GUILayout.Label("?", GUILayout.ExpandWidth(expand: false));
				if (IsHover()) result = true;
			}

			GUILayout.EndHorizontal();
			return result;
		}

		public static void DrawToggle(ref bool value, string title, bool vertical = false)
		{
			DrawToggleWithTooltip(ref value, title, false, vertical);
		}

		public static bool DrawToggleWithTooltip(ref bool value, string title, bool tooltip = true, bool vertical = false)
		{
			if (vertical) GUILayout.BeginVertical();
			else GUILayout.BeginHorizontal();

			GUILayout.Label(title, GUILayout.ExpandWidth(expand: false));
			if (!vertical) GUILayout.Space(5);
			value = GUILayout.Toggle(value, "", GUILayout.ExpandWidth(expand: false));
			bool result = false;
			if (tooltip)
			{
				GUILayout.Label("?", GUILayout.ExpandWidth(expand: false));
				if (IsHover()) result = true;
			}

			if (vertical) GUILayout.EndVertical();
			else GUILayout.EndHorizontal();
			return result;
		}

		public static void PopupToggleGroup<T>(ref T selected, string title, bool vertical = false) where T : Enum
		{
			PopupToggleGroupWithTooltip(ref selected, title, false, vertical);
		}

		public static bool PopupToggleGroupWithTooltip<T>(ref T selected, string title, bool tooltip = true, bool vertical = false) where T : Enum
		{
			if (vertical) GUILayout.BeginVertical();
			else GUILayout.BeginHorizontal();

			GUILayout.Label(title, GUILayout.ExpandWidth(expand: false));
			if (!vertical) GUILayout.Space(5);
			var names2 = Enum.GetNames(typeof(T));
			int selectedInt = (int)(object)selected;
			if (UnityModManager.UI.PopupToggleGroup(ref selectedInt, names2, title, 0, null, GUILayout.ExpandWidth(expand: false)))
			{
				object value = Enum.Parse(typeof(T), names2[selectedInt]);
				selected = (T)value;
			}
			bool result = false;
			if (tooltip)
			{
				GUILayout.Label("?", GUILayout.ExpandWidth(expand: false));
				if (IsHover()) result = true;
			}

			if (vertical) GUILayout.EndVertical();
			else GUILayout.EndHorizontal();
			return result;
		}
	}
}
