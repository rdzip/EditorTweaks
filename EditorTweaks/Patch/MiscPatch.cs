using ADOFAI;
using ADOFAI.Editor.Components;
using ADOFAI.Editor.Components.Gradients;
using ADOFAI.Editor.Interfaces;
using ADOFAI.Editor.Models;
using ADOFAI.LevelEditor.Controls;
using EditorTweaks.Utils;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTweaks.Patch
{
	[HarmonyPatch]
	public static class MiscPatch
	{
		private static GameObject horizontalProperty;
		private static GameObject verticalProperty;

		[HarmonyPatch(typeof(scnEditor), "Awake")]
		[HarmonyPrefix]
		public static void SetHorizontalProperty()
		{
			if (verticalProperty == null)
			{
				GameObject parentObject = ETUtils.FindOrCreatePrefabsObject();

				verticalProperty = ADOBase.gc.prefab_property;

				horizontalProperty = GameObject.Instantiate(ADOBase.gc.prefab_property, parentObject.transform);
				GameObject.DestroyImmediate(horizontalProperty.GetComponent<VerticalLayoutGroup>());

				var layout = horizontalProperty.AddComponent<HorizontalLayoutGroup>();
				layout.childControlWidth = true;
				layout.childControlHeight = true;
				layout.childForceExpandWidth = false;
				layout.childForceExpandHeight = true;
				layout.spacing = 8;

				GameObject labelArea = horizontalProperty.transform.Find("labelArea").gameObject;
				labelArea.transform.Find("label").gameObject.GetComponent<LayoutElement>().flexibleWidth = -1;

				GameObject enableButton = horizontalProperty.transform.Find("enableButton").gameObject;
				RectTransform enableButtonRT = enableButton.GetComponent<RectTransform>();
				enableButtonRT.SetSiblingIndex(1);

				horizontalProperty.transform.Find("controlContainer").Find("Off").GetComponent<TextMeshProUGUI>().raycastTarget = false;
			}

			ADOBase.gc.prefab_property = Main.ETConfig.horizontalProperty ? horizontalProperty : verticalProperty;
		}

		[HarmonyPatch(typeof(scnEditor), "Update")]
		[HarmonyPostfix]
		public static void ShowCoordinate(scnEditor __instance)
		{
			if (Input.GetKeyDown(KeyCode.F3)) CoordinateText.Show = !CoordinateText.Show;
			if (!CoordinateText.Show) return;
			float o = Camera.current.orthographicSize;
			var mousePosition = __instance.camera.ScreenToWorldPoint(Input.mousePosition).xy() /
								new Vector2(1.5f, 1.5f);
			var position = Camera.current.transform.position / new Vector2(1.5f, 1.5f);
			CoordinateText.Text = $"Camera Position: ({Math.Round(position.x, 6)}, {Math.Round(position.y, 6)})\nMouse Position: ({Math.Round(mousePosition.x, 6)}, {Math.Round(mousePosition.y, 6)})\nZoom: {scrCamera.instance.zoomSize * 100f,3}%\nEditor Zoom: {Math.Round(scrCamera.instance.userSizeMultiplier * 100f, 3)}%";
		}

		[HarmonyPatch(typeof(RDColorPickerPopup), nameof(RDColorPickerPopup.Show))]
		[HarmonyPostfix]
		public static void InstantApplyColor(RDColorPickerPopup __instance)
		{
			if (!Main.ETConfig.instantApplyColor)
			{
				__instance.cuiColorPicker.SetOnValueChangeCallback(null);
			}
			else
			{
				__instance.cuiColorPicker.SetOnValueChangeCallback((color) =>
				{
					__instance.Get<IColorPickerData>("colorPC").Get<ColorField>("control").onChange.Invoke("-" + ColorUtility.ToHtmlStringRGBA(color));
				});
			}
		}

		[HarmonyPatch(typeof(GradientEditor), "OnColorChange")]
		[HarmonyPrefix]
		public static void FixGradientEditor(ref string color)
		{
			if (color.StartsWith("-"))
				color = color.Substring(1);
		}

		[HarmonyPatch(typeof(PropertyControl_Color), nameof(PropertyControl_Color.OnChange))]
		[HarmonyPrefix]
		public static bool CheckSaveOnChange(PropertyControl_Color __instance, string s)
		{
			if (s.StartsWith("-"))
			{
				s = s.Substring(1);
				DontSaveOnChange(__instance, s);
				return false;
			}
			else
			{
				return true;
			}
		}

		public static void DontSaveOnChange(PropertyControl_Color color, string s)
		{
			LevelEvent selectedEvent = color.propertiesPanel.inspectorPanel.selectedEvent;
			string original = selectedEvent[color.propertyInfo.name] as string;
			selectedEvent[color.propertyInfo.name] = s;
			if (selectedEvent.eventType == LevelEventType.BackgroundSettings)
			{
				ADOBase.customLevel.SetBackground();
			}
			else if (selectedEvent.IsDecoration)
			{
				ADOBase.editor.UpdateDecorationObject(selectedEvent);
			}
			if (color.propertyInfo.affectsFloors)
			{
				ADOBase.editor.ApplyEventsToFloors();
			}
			color.OnValueChange();
			selectedEvent[color.propertyInfo.name] = original;
		}

		[HarmonyPatch(typeof(PropertyControl_MinMaxGradient), "Save")]
		[HarmonyPrefix]
		public static bool CheckSaveOnChange(PropertyControl_MinMaxGradient __instance, SerializedMinMaxGradient value)
		{
			if ((value.color1 != null && value.color1.StartsWith("-")) || (value.color2 != null && value.color2.StartsWith("-")))
			{
				if (value.color1 != null)
					value.color1 = value.color1.StartsWith("-") ? value.color1.Substring(1) : value.color1;
				if (value.color2 != null)
					value.color2 = value.color2.StartsWith("-") ? value.color2.Substring(1) : value.color2;
				DontSaveOnChange(__instance, value);
				return false;
			}
			else
			{
				return true;
			}
		}

		public static void DontSaveOnChange(PropertyControl_MinMaxGradient gradient, SerializedMinMaxGradient color)
		{
			gradient.Set("current", color);
			SerializedMinMaxGradient original = (SerializedMinMaxGradient)gradient.propertiesPanel.inspectorPanel.selectedEvent[gradient.propertyInfo.name];
			gradient.propertiesPanel.inspectorPanel.selectedEvent[gradient.propertyInfo.name] = color;
			gradient.ToggleOthersEnabled();
			gradient.OnValueChange();
			gradient.propertiesPanel.inspectorPanel.selectedEvent[gradient.propertyInfo.name] = original;
		}

		public static void OnUnpatch()
		{
			if (verticalProperty == null) return;
			ADOBase.gc.prefab_property = verticalProperty;
		}
	}
}
