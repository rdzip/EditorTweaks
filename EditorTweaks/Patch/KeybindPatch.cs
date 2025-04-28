using ADOFAI.Editor.Actions;
using ADOFAI.Editor.Preferences.Models;
using ADOFAI.Editor.Preferences;
using ADOFAI.Editor;
using EditorTweaks.ADOFAI.EditorActions;
using EditorTweaks.ADOFAI.EditorPreferencesControls;
using EditorTweaks.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine;

namespace EditorTweaks.Patch
{
	[HarmonyPatch]
	public static class KeybindPatch
	{
		public static readonly Type[] IgnoreActions = new Type[]
		{
			typeof(AddNumberedEventEditorAction),
			typeof(SelectEventCategoryEditorAction),
			typeof(ShowCopyrightPopupEditorAction),
			typeof(OpenLogDirectoryEditorAction),
			typeof(CreateFloorWithCharOrAngleEditorAction),
			typeof(ToggleFileActionsPanelEditorAction)
		};
		public static Dictionary<EditorAction, List<EditorKeybind>> EditorActionKeybinds = new Dictionary<EditorAction, List<EditorKeybind>>();
		private static EditorPreferencesKeybind _currentEditingKeybind;
		private static GameObject RaycastObject;
		public static EditorPreferencesKeybind CurrentEditingKeybind
		{
			get { return _currentEditingKeybind; }
			set
			{
				if (_currentEditingKeybind != null)
					_currentEditingKeybind.CancelKeybind();
				_currentEditingKeybind = value;
				if (value == null) RaycastObject?.SetActive(false);
				else RaycastObject?.SetActive(true);
			}
		}

		[HarmonyPatch(typeof(scnEditor), "RegisterKeybinds")]
		[HarmonyPostfix]
		public static void AddKeybind(scnEditor __instance)
		{
			//TODO: Patch scnEditor.SetupHelpMenu().
			EditorKeybindManager keybindManager = __instance.Get<EditorKeybindManager>("keybindManager");

			EditorActionKeybinds = new Dictionary<EditorAction, List<EditorKeybind>>();

			foreach (var kv in keybindManager.dictionary)
			{
				EditorKeybind keybind = kv.Key;
				List<EditorAction> actions = kv.Value;

				foreach (var action in actions)
				{
					bool exist = false;
					foreach (var ac in EditorActionKeybinds.Keys)
					{
						if (ac.GetType() == action.GetType())
						{
							if (ac.GetType() == typeof(CreateFloorWithCharOrAngleEditorAction))
							{
								if (ac.Get<float>("angle") != action.Get<float>("angle") ||
									ac.Get<char>("chara") != action.Get<char>("chara") ||
									ac.Get<bool>("fullSpin") != action.Get<bool>("fullSpin")) continue;
							}
							else if (ac.GetType() == typeof(AddNumberedEventEditorAction) || ac.GetType() == typeof(SelectEventCategoryEditorAction))
							{
								if (ac.Get<int>("number") != action.Get<int>("number")) continue;
							}
							EditorActionKeybinds[ac].Add(keybind);
							if (EditorActionKeybinds[ac].Count > 2)
								ETLogger.Warn(ac.ToString() + " has more than 2 shortcuts. "
									+ EditorActionKeybinds[ac].Count);
							exist = true;
						}
					}
					if (!exist)
						EditorActionKeybinds.Add(action, new List<EditorKeybind> { keybind });
				}
			}

			keybindManager.RegisterKeybind(new EditorKeybind(KeyModifier.Shift, KeyCode.Minus, true), new ZoomOutCameraEditorActionEx());
			keybindManager.RegisterKeybind(new EditorKeybind(KeyModifier.Shift, KeyCode.KeypadMinus, true), new ZoomOutCameraEditorActionEx());
			keybindManager.RegisterKeybind(new EditorKeybind(KeyModifier.Shift, KeyCode.Equals, true), new ZoomInCameraEditorActionEx());
			keybindManager.RegisterKeybind(new EditorKeybind(KeyModifier.Shift, KeyCode.KeypadPlus, true), new ZoomInCameraEditorActionEx());
		}

		static void KeybindCategoryCallback(EditorPreferencesCategory category)
		{
			foreach (var kv in EditorActionKeybinds.OrderBy(item => item.Key.sectionKey))
			{
				var Action = kv.Key;
				var Keybinds = kv.Value;

				if (IgnoreActions.Contains(Action.GetType())) continue;

				category.AddEntry(new EditorPreferencesEntry("editortweaks." + Action.descriptionKey,
					new EditorPreferencesKeybind(() => Keybinds, null
				)));
			}

			RaycastObject = new GameObject("Blocker");
			var canvas = RaycastObject.AddComponent<Canvas>();
			canvas.sortingOrder = 1000;
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			RaycastObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1600, 900);
			var image = RaycastObject.AddComponent<Image>();
			image.raycastTarget = true;
			image.color = new Color(0f, 0f, 0f, 0f);
			RaycastObject.AddComponent<GraphicRaycaster>();
			RaycastObject.SetActive(false);
		}

		public static void SetupKeybindsMenu(EditorPreferencesMenu __instance)
		{
			EditorPreferencesKeybind.ButtonTemplate = __instance.tabButtonTemplate;
			var delType = typeof(EditorPreferencesMenu).GetNestedType("AddCategoryDelegate",
				System.Reflection.BindingFlags.NonPublic);

			var methodInfo = typeof(KeybindPatch)
				.GetMethod(nameof(KeybindPatch.KeybindCategoryCallback), BindingFlags.NonPublic | BindingFlags.Static);
			var callback = Delegate.CreateDelegate(delType, methodInfo);
			//__instance.Call("AddCategory", new object[] { "editortweaks.keybind", callback });
		}

		[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.userIsEditingAnInputField), MethodType.Getter)]
		[HarmonyPostfix]
		public static void ForceEditingInputField(ref bool __result)
		{
			if (CurrentEditingKeybind != null) __result = true;
		}

		[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.HidePreferences))]
		[HarmonyPostfix]
		public static void OnHidePreferences()
		{
			CurrentEditingKeybind = null;
		}

		[HarmonyPatch(typeof(EditorPreferencesMenu), nameof(EditorPreferencesMenu.SelectCategory))]
		[HarmonyPrefix]
		public static void OnCategoryChange()
		{
			CurrentEditingKeybind = null;
		}

		[HarmonyPatch(typeof(EditorPreferencesMenu), "Update")]
		[HarmonyPostfix]
		public static void OnUpdate()
		{
			if (CurrentEditingKeybind == null) return;

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				CurrentEditingKeybind.FinishKeybind(new EditorKeybind(KeyCode.None));
				CurrentEditingKeybind = null;
			}

			KeyModifier modifier = KeyModifier.None;
			bool ctrlIsCmd = true;
			KeyCode keyCode = KeyCode.None;
			bool finishInput = false;

			GetKeys();

			if (Input.anyKeyDown)
			{
				if (keyCode != KeyCode.None)
				{
					CurrentEditingKeybind.ChangingKeybind(new EditorKeybind(modifier, keyCode, ctrlIsCmd));
				}
			}

			if (finishInput)
			{
				if (keyCode != KeyCode.None)
				{
					CurrentEditingKeybind.FinishKeybind(new EditorKeybind(modifier, keyCode, ctrlIsCmd));
					CurrentEditingKeybind = null;
				}
			}

			void GetKeys()
			{
				if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
				{
					modifier |= KeyModifier.Control;
					if (ADOBase.platform == Platform.Mac)
						ctrlIsCmd = false;
				}
				if (Input.GetKey(KeyCode.LeftMeta) || Input.GetKey(KeyCode.RightMeta))
				{
					modifier |= KeyModifier.Control;
					ctrlIsCmd = true;
				}
				if (RDInput.holdingShift)
					modifier |= KeyModifier.Shift;
				if (RDInput.holdingAlt)
					modifier |= KeyModifier.Alt;

				foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
				{
					if (Input.GetKey(key))
					{
						if (key == KeyCode.LeftControl || key == KeyCode.RightControl ||
							key == KeyCode.LeftMeta || key == KeyCode.RightMeta ||
							key == KeyCode.LeftShift || key == KeyCode.RightShift ||
							key == KeyCode.LeftAlt || key == KeyCode.RightAlt ||
							(key >= KeyCode.AltGr && key <= KeyCode.Break))
						{
							continue;
						}
						keyCode = key;
						finishInput = true;
					}
				}
			}
		}
	}
}
