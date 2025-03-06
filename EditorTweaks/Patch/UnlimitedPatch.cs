using ADOFAI;
using ADOFAI.Editor.Components;
using ADOFAI.LevelEditor.Controls;
using EditorTweaks.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTweaks.Patch
{
	[HarmonyPatch]
	public static class UnlimitedPatch
	{
		[HarmonyPatch]
		public static class ValidatePatch
		{
			static bool OutOfRange_Text = false;
			static bool OutOfRange_Vector2 = false;
			static bool OutOfRange_Tuple = false;

			static void SetWarnButton(PropertyControl control, bool Active, string message = "")
			{
				try
				{
					if (control is PropertyControl_FilterProperties) return;

					DraggableNumberInputField draggable;
					if ((draggable = control.gameObject.GetComponent<DraggableNumberInputField>()) != null)
					{
						draggable.clamp = false;
					}

					var gameObj = control.propertyTransform.gameObject;
					var label = gameObj.transform.Find("labelArea").gameObject;
					GameObject warn;
					if (label.transform.Find("warn") == null)
					{
						if (!Active) return;
						var help = label.transform.Find("help").gameObject;
						warn = UnityEngine.Object.Instantiate(help, help.transform.parent);
						warn.name = "warn";
						warn.SetActive(true);
						warn.transform.SetSiblingIndex(1);
						var button = warn.transform.GetChild(0).gameObject;
						button.SetActive(true);
						button.GetComponent<Image>().color = new Color(1f, 0.75f, 0f);
						button.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "!";
					}
					else
						warn = label.transform.Find("warn").gameObject;

					Button btn = warn.transform.GetChild(0).GetComponent<Button>();
					btn.onClick.RemoveAllListeners();
					btn.onClick.AddListener(() => ADOBase.editor.ShowPropertyHelp(true, btn.transform, message, "", ""));
					warn.SetActive(Active);
				}
				catch (Exception ex)
				{
					//ETLogger.Error(ex);
				}
			}

			[HarmonyPatch(typeof(PropertyInfo), nameof(PropertyInfo.Validate), new Type[] { typeof(float) })]
			[HarmonyPostfix]
			public static void ValidateFloat(PropertyInfo __instance, float value, ref float __result)
			{
				if (__result != value)
				{
					OutOfRange_Text = true;
				}
				__result = value;
			}

			[HarmonyPatch(typeof(PropertyInfo), nameof(PropertyInfo.Validate), new Type[] { typeof(int) })]
			[HarmonyPostfix]
			public static void ValidateInt(PropertyInfo __instance, int value, ref int __result)
			{
				if (__result != value)
				{
					OutOfRange_Text = true;
				}
				__result = value;
			}

			[HarmonyPatch(typeof(PropertyInfo), nameof(PropertyInfo.Validate), new Type[] { typeof(Vector2), typeof(bool) })]
			[HarmonyPostfix]
			public static void ValidateVector2(PropertyInfo __instance, Vector2 value, ref Vector2 __result)
			{
				if (__result != value)
				{
					OutOfRange_Vector2 = true;
				}
				__result = value;
			}

			[HarmonyPatch(typeof(PropertyInfo), nameof(PropertyInfo.Validate), new Type[] { typeof(Tuple<float, float>) })]
			[HarmonyPostfix]
			public static void ValidateTuple(PropertyInfo __instance, Tuple<float, float> value, ref Tuple<float, float> __result)
			{
				if (__result != value)
				{
					OutOfRange_Tuple = true;
				}
				__result = value;
			}

			[HarmonyPatch(typeof(PropertyControl_Text), nameof(PropertyControl_Text.Validate))]
			[HarmonyPrefix]
			public static void PreValidateText()
			{
				OutOfRange_Text = false;
			}

			[HarmonyPatch(typeof(PropertyControl_Text), nameof(PropertyControl_Text.Validate))]
			[HarmonyPostfix]
			public static void PostValidateText(PropertyControl_Text __instance)
			{
				//TODO: Reason
				string min = "";
				string max = "";
				switch (__instance.propertyInfo.type)
				{
					case PropertyType.Int:
						min = __instance.propertyInfo.int_min.ToString();
						max = __instance.propertyInfo.int_max.ToString();
						break;
					case PropertyType.Float:
						min = __instance.propertyInfo.float_min.ToString();
						max = __instance.propertyInfo.float_max.ToString();
						break;
				}

				SetWarnButton(__instance, OutOfRange_Text, $"Out of range!\nMin: {min}\nMax: {max}");
			}

			[HarmonyPatch(typeof(PropertyControl_Vector2), nameof(PropertyControl_Vector2.Validate))]
			[HarmonyPrefix]
			public static void PreValidateVector2()
			{
				OutOfRange_Vector2 = false;
			}

			[HarmonyPatch(typeof(PropertyControl_Vector2), nameof(PropertyControl_Vector2.Validate))]
			[HarmonyPostfix]
			public static void PostValidateVector2(PropertyControl_Vector2 __instance)
			{
				string min = __instance.propertyInfo.minVec.ToString();
				string max = __instance.propertyInfo.maxVec.ToString();
				SetWarnButton(__instance, OutOfRange_Vector2, $"Out of range!\nMin: {min}\nMax: {max}");
			}

			[HarmonyPatch(typeof(PropertyControl_FloatPairBase), nameof(PropertyControl_FloatPairBase.Validate))]
			[HarmonyPrefix]
			public static void PreValidateFloatPair()
			{
				OutOfRange_Tuple = false;
			}

			[HarmonyPatch(typeof(PropertyControl_FloatPairBase), nameof(PropertyControl_FloatPairBase.Validate))]
			[HarmonyPostfix]
			public static void PostValidateFloatPair(PropertyControl_Vector2 __instance)
			{
				string min = __instance.propertyInfo.floatPairMin.ToString();
				string max = __instance.propertyInfo.floatPairMax.ToString();
				SetWarnButton(__instance, OutOfRange_Tuple, $"Out of range!\nMin: {min}\nMax: {max}");
			}

			[HarmonyPatch(typeof(RDColorPickerPopup), "UsesAlpha", MethodType.Getter)]
			[HarmonyPostfix]
			public static void UsesAlpha(ref bool __result)
			{
				__result = true;
			}

			[HarmonyPatch(typeof(ColorField), "Validate")]
			[HarmonyPrefix]
			public static void ValidateColor(ColorField __instance)
			{
				__instance.usesAlpha = true;
			}

			[HarmonyPatch(typeof(PropertyControl_Color), nameof(PropertyControl_Color.ValidateInput))]
			[HarmonyPostfix]
			public static void ValidateColorControl(PropertyControl_Color __instance)
			{
				if (!__instance.propertyInfo.color_usesAlpha)
				{
					if (__instance.colorField.value.Length == 8) // && __result.Substring(6).ToLower() != "ff"
						SetWarnButton(__instance, true, "Alpha is not originally available here.");
					else
						SetWarnButton(__instance, false);
				}
			}
		}

		[HarmonyPatch]
		public static class FirstFloorPatch
		{
			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.UpdateCategoryVisibility))]
			[HarmonyPrefix]
			public static bool ForceShowCategory()
			{
				return false;
			}

			[HarmonyPatch(typeof(LevelEventInfo), nameof(LevelEventInfo.allowFirstFloorCheck), MethodType.Getter)]
			[HarmonyPostfix]
			public static void ForceAllowFirstFloor(ref bool __result)
			{
				__result = true;
			}
		}

		[HarmonyPatch]
		public static class EditorZoomPatch
		{
			public static float ClampMin()
			{
				//0.5f
				return 0.5f;
			}

			public static float ClampMax()
			{
				//15f
				return float.MaxValue;
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ZoomCamera))]
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> ZoomCameraPatch(IEnumerable<CodeInstruction> instructions)
			{
				int opIdx = -1;

				var codes = new List<CodeInstruction>(instructions);
				var pattern = new List<CodeInstruction>()
				{
					new CodeInstruction(OpCodes.Ldc_R4, 0.5f),
					new CodeInstruction(OpCodes.Ldc_R4, 15f),
					new CodeInstruction(OpCodes.Call, typeof(Mathf).GetMethod("Clamp",
						new Type[] { typeof(float), typeof(float), typeof(float) }))
				};
				var replace = new List<CodeInstruction>()
				{
					new CodeInstruction(OpCodes.Call, typeof(EditorZoomPatch).GetMethod("ClampMin")),
					new CodeInstruction(OpCodes.Call, typeof(EditorZoomPatch).GetMethod("ClampMax")),
					null
				};

				for (int i = 0, fnd = 0; i < codes.Count; i++)
				{
					var code = codes[i];

					if (code.opcode == pattern[fnd].opcode && (pattern[fnd].operand == null ||
						((bool)code.operand?.GetType().GetMethod("Equals", new[] { code.operand.GetType() }).Invoke(code.operand, new[] { pattern[fnd].operand }))))
						fnd++;
					else
						fnd = 0;
					if (fnd == 3)
					{
						opIdx = i - 2;
						break;
					}
				}

				if (opIdx == -1 || pattern.Count != replace.Count)
				{
					ETLogger.Error("Patch Failed(ZoomCamera).");
				}
				else
				{
					for (int i = 0; i < pattern.Count; i++)
					{
						if (replace[i] != null) codes[opIdx + i] = replace[i];
					}
				}

				return codes.AsEnumerable();
			}
		}
	}
}
