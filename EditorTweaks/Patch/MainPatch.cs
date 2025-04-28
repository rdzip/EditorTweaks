using HarmonyLib;
using SA.GoogleDoc;
using System;
using System.Globalization;

namespace EditorTweaks.Patch
{
	[HarmonyPatch]
	public static class MainPatch
	{
		[HarmonyPatch(typeof(scnEditor), "Start")]
		[HarmonyPostfix]
		public static void OnEditorLoaded(scnEditor __instance)
		{

		}

		[HarmonyPatch(typeof(LocalizationClient), nameof(LocalizationClient.GetLocalizedString),
			new Type[] { typeof(string), typeof(LangSection), typeof(LangCode) })]
		[HarmonyPrefix]
		public static bool Localization(string token, LangSection section, LangCode language, ref string __result)
		{
			if (!token.Contains("editortweaks.")) return true;
			if (token.StartsWith("editortweaks."))
			{
				//TODO: return string.
				if (token == "editortweaks.dialog.differentTypeEventSelected")
					__result = "Different types of events have been selected. To change their properties, only select events of the same type.";
				if (token == "editortweaks.reorderWarning")
					__result = "Reorder timeline. Are you sure?";
				return false;
			}
			else if (token.StartsWith("editor.prefs.fields.editortweaks."))
			{
				token = token.Replace("editor.prefs.fields.editortweaks.", "editor.shortcuts.");
				bool exists = false;
				__result = RDString.GetWithCheck(token, out exists);
				if (!exists)
				{
					token = token.Replace("editor.shortcuts.", "editortweaks.");
					//TODO: return string.
					if (token == "editortweaks.general.horizontal_property")
						__result = "Enable horizontal properties";
					else if (token == "editortweaks.general.horizontal_property.description")
						__result = "Need Restart Editor!!!";
					else if (token == "editortweaks.general.instant_apply_color")
						__result = "Apply color instantly";
					else if (token == "editortweaks.general.instant_apply_color.description")
						__result = "It may cause a frame drops.";
					else if (token == "editortweaks.timeline.horizontal_scroll_direction_invert")
						__result = "Invert timeline horizontal scroll";
					else if (token == "editortweaks.timeline.jump_to_floor")
						__result = "Jump to floor when floor is selected";
					else if (token == "editortweaks.timeline.jump_to_event")
						__result = "Jump to event when event is selected";
				}
				return false;
			}
			else if (token.StartsWith("editor.prefs.tabs.editortweaks."))
			{
				int len = "editor.prefs.tabs.editortweaks.".Length;
				token = token.Substring(len, token.Length - len);
				if (token == "editorTweaks")
					__result = "EditorTweaks";
				else if (token == "timeline")
					__result = "Timeline";
				else
					__result = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(token);
				return false;
			}
			return true;
		}

		[HarmonyPatch(typeof(LocalizationClient), nameof(LocalizationClient.ExistsLocalizedString))]
		[HarmonyPostfix]
		public static void ExistsLocalization(string token, ref bool __result)
		{
			if (token.Contains("editortweaks.")) __result = true;
		}
	}
}
