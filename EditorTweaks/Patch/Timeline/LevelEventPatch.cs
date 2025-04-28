using ADOFAI;
using EditorTweaks.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorTweaks.Patch.Timeline
{
	[HarmonyPatch]
	public static class LevelEventPatch
	{
		public static bool shouldOptimizeRows = false;
		public static bool shouldApply = true;

		[HarmonyPatch(typeof(ADOStartup), nameof(ADOStartup.DecodeLevelEventInfoList))]
		[HarmonyPostfix]
		public static void TimelineRowInfo(ref Dictionary<string, LevelEventInfo> __result)
		{
			foreach (var kv in __result)
			{
				if (!TimelineConstants.TimelineIgnoreEvents.Contains(kv.Value.type))
				{
					PropertyInfo property = new PropertyInfo(new Dictionary<string, object>
					{
						{ "name", "row" },
						{ "type", "String" },
						{ "default", "-1:-1" },
						{ "control", "Hidden" }
					}, kv.Value);

					kv.Value.propertiesInfo.Add("row", property);
				}
			}
		}

		[HarmonyPatch(typeof(LevelEvent), nameof(LevelEvent.Decode))]
		[HarmonyPostfix]
		public static void TimelineRowCheck(LevelEvent __instance)
		{
			if (ETUtils.GetRowNumber(__instance) >= 0)
				shouldOptimizeRows = false;
		}

		[HarmonyPatch(typeof(LevelData), nameof(LevelData.LoadLevel))]
		[HarmonyPrefix]
		public static void OnPreLoadLevel()
		{
			if (!ADOBase.isInternalLevel)
			{
				shouldOptimizeRows = true;
			}
		}

		[HarmonyPatch(typeof(LevelData), nameof(LevelData.LoadLevel))]
		[HarmonyPostfix]
		public static void OnPostLoadLevel(bool __result)
		{
			shouldApply = !ADOBase.isInternalLevel && __result;
		}
	}
}
