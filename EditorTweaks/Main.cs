using EditorTweaks.Patch;
using EditorTweaks.Utils;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace EditorTweaks
{
	public class Main
	{
		public static Harmony Patch;
		public static int GameVersion = 0;
		public static string ModPath = "";
		public static UnityModManager.ModEntry Entry;
		public static Config ETConfig;

		public static void Load(UnityModManager.ModEntry modEntry)
		{
			ADOStartup.ModWasAdded(modEntry.Info.Id);
#if DEBUG
			ETLogger.Setup(modEntry.Logger, ETLogger.LogLevel.Debug);
#else
			ETLogger.Setup(modEntry.Logger, ETLogger.LogLevel.Info);
#endif
			GameVersion = (int)AccessTools.Field(typeof(GCNS), nameof(GCNS.releaseNumber)).GetValue(null);
			ETLogger.Info($"Game Version: {GameVersion}");
			ModPath = modEntry.Path;
			Patch = new Harmony(modEntry.Info.Id);
			Entry = modEntry;
			ETConfig = UnityModManager.ModSettings.Load<Config>(modEntry);

			modEntry.OnToggle = (entry, value) =>
			{
				if (value)
				{
					Patch.PatchAll(Assembly.GetExecutingAssembly());
					CoordinateText.Instance = new GameObject("ET_CoordinateText").AddComponent<CoordinateText>();
					GameObject.DontDestroyOnLoad(CoordinateText.Instance);
					scrSfx.instance?.PlaySfx(ADOBase.gc.soundEffects[10], MixerGroup.InterfaceParent, 1f, 1f, 0f);
				}
				else
				{
					Patch.UnpatchAll(entry.Info.Id);
					GameObject.Destroy(CoordinateText.Instance);
					CoordinateText.Instance = null;
					MiscPatch.OnUnpatch();
					scrSfx.instance?.PlaySfx(ADOBase.gc.soundEffects[11], MixerGroup.InterfaceParent, 1f, 1f, 0f);
				}

				return true;
			};

			modEntry.OnSaveGUI = (entry) =>
			{
				UnityModManager.ModSettings.Save<Config>(ETConfig, entry);
			};
		}
	}
}
