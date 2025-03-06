using System;
using UnityModManagerNet;

namespace EditorTweaks.Utils
{
	internal static class ETLogger
	{
		internal enum LogLevel
		{
			Debug,
			Info,
			Warn,
			Error,
			None
		}

		private static UnityModManager.ModEntry.ModLogger Logger;
		private static LogLevel Level;

		internal static void Setup(UnityModManager.ModEntry.ModLogger logger, LogLevel level)
		{
			ETLogger.Logger = logger;
			ETLogger.Level = level;
		}

		private static void Log(LogLevel level, string message)
		{
			if (level < ETLogger.Level || level == LogLevel.None) return;
			// [12:34:56.789] INFO: message

			Logger.Log($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] {level.ToString().ToUpper()}: {message}");
		}

		internal static void Debug(string message)
		{
			Log(LogLevel.Debug, message);
		}

		internal static void Debug(object obj)
		{
			Log(LogLevel.Debug, obj.ToString());
		}

		internal static void Info(string message)
		{
			Log(LogLevel.Info, message);
		}

		internal static void Info(object obj)
		{
			Log(LogLevel.Info, obj.ToString());
		}

		internal static void Warn(string message)
		{
			Log(LogLevel.Warn, message);
		}

		internal static void Warn(object obj)
		{
			Log(LogLevel.Warn, obj.ToString());
		}

		internal static void Error(string message)
		{
			Log(LogLevel.Error, message);
		}

		internal static void Error(object obj)
		{
			Log(LogLevel.Error, obj.ToString());
		}
	}
}
