using ADOFAI.Editor.Preferences;
using ADOFAI.Editor.Preferences.Controls;
using ADOFAI.Editor.Preferences.Models;
using EditorTweaks.Utils;
using HarmonyLib;
using System;
using System.Reflection;

namespace EditorTweaks.Patch
{
	[HarmonyPatch]
	public static class PreferencePatch
	{
		static void CategoryCallback(EditorPreferencesCategory category)
		{
			category.AddEntry(new EditorPreferencesEntry("editortweaks.general.horizontal_property", new EditorPreferencesToggle(() => Main.ETConfig.horizontalProperty, (v) =>
			{
				Main.ETConfig.horizontalProperty = v;
				Main.Entry.OnSaveGUI(Main.Entry);
			})));
			category.AddEntry(new EditorPreferencesEntry("editortweaks.general.instant_apply_color", new EditorPreferencesToggle(() => Main.ETConfig.instantApplyColor, (v) =>
			{
				Main.ETConfig.instantApplyColor = v;
				Main.Entry.OnSaveGUI(Main.Entry);
			})));
			category.AddEntry(new EditorPreferencesEntry("editortweaks.timeline.horizontal_scroll_direction_invert", new EditorPreferencesToggle(() => Main.ETConfig.timelineHorizontalScrollDirectionInvert, (v) =>
			{
				Main.ETConfig.timelineHorizontalScrollDirectionInvert = v;
				Main.Entry.OnSaveGUI(Main.Entry);
			})));
		}

		[HarmonyPatch(typeof(EditorPreferencesMenu), "SetupMenu")]
		[HarmonyPostfix]
		public static void SetupPreferenceMenu(EditorPreferencesMenu __instance)
		{
			var delType = typeof(EditorPreferencesMenu).GetNestedType("AddCategoryDelegate",
				System.Reflection.BindingFlags.NonPublic);

			var methodInfo = typeof(PreferencePatch).GetMethod(nameof(CategoryCallback), BindingFlags.NonPublic | BindingFlags.Static);
			var callback = Delegate.CreateDelegate(delType, methodInfo);
			__instance.Call("AddCategory", new object[] { "editortweaks.editorTweaks", callback });

			KeybindPatch.SetupKeybindsMenu(__instance);
		}
	}
}
