using ADOFAI;
using System.Collections.Generic;
using UnityEngine;

namespace EditorTweaks.Patch.Timeline
{
	public static class TimelineConstants
	{
		public static readonly LevelEventType[] TimelineIgnoreEvents = new LevelEventType[]
		{
			LevelEventType.None,
			LevelEventType.SetSpeed,
			LevelEventType.Twirl,
			LevelEventType.Checkpoint,
			LevelEventType.LevelSettings,
			LevelEventType.SongSettings,
			LevelEventType.TrackSettings,
			LevelEventType.BackgroundSettings,
			LevelEventType.CameraSettings,
			LevelEventType.MiscSettings,
			LevelEventType.EventSettings,
			LevelEventType.DecorationSettings,
			LevelEventType.AddDecoration,
			LevelEventType.AddText,
			LevelEventType.Hold,
			LevelEventType.CallMethod,
			LevelEventType.AddComponent,
			LevelEventType.MultiPlanet,
			LevelEventType.FreeRoam,
			LevelEventType.FreeRoamTwirl,
			LevelEventType.FreeRoamRemove,
			LevelEventType.FreeRoamWarning,
			LevelEventType.Pause,
			LevelEventType.AutoPlayTiles,
			LevelEventType.ScaleMargin,
			LevelEventType.ScaleRadius,
			LevelEventType.Multitap,
			LevelEventType.TileDimensions,
			LevelEventType.KillPlayer,
			LevelEventType.SetFloorIcon,
			LevelEventType.AddObject,
			LevelEventType.AddParticle
		};

		public static readonly Color DefaultEventColor = new Color(0.690196f, 0.690196f, 0.690196f);
		public static readonly Color SoundEventColor = new Color(1f, 0.196078f, 0.196078f);
		public static readonly Color PlanetEventColor = new Color(0.196078f, 0.470588f, 1f);
		public static readonly Color TrackEventColor = new Color(1f, 0.870588f, 0.415686f);
		public static readonly Color DecorationEventColor = new Color(0.435294f, 0.933333f, 0.423529f);
		public static readonly Color CameraEventColor = new Color(1f, 0.415686f, 0.941176f);
		public static readonly Color FilterEventColor = new Color(0.368627f, 0.886274f, 1f);
		public static readonly Color ModifierEventColor = new Color(1f, 0.709803f, 0.368627f);
		public static readonly Color CommentEventColor = new Color(0.760784f, 0.368627f, 1f);

		public static readonly Dictionary<LevelEventType, Color> EventColors = new Dictionary<LevelEventType, Color>()
		{
			{ LevelEventType.None, DefaultEventColor },
			{ LevelEventType.SetHitsound, SoundEventColor },
			{ LevelEventType.PlaySound, SoundEventColor },
			{ LevelEventType.SetHoldSound, SoundEventColor },
			{ LevelEventType.SetPlanetRotation, PlanetEventColor },
			{ LevelEventType.ScalePlanets, PlanetEventColor },
			{ LevelEventType.ScaleRadius, PlanetEventColor },
			{ LevelEventType.ScaleMargin, PlanetEventColor },
			{ LevelEventType.ChangeTrack, TrackEventColor },
			{ LevelEventType.ColorTrack, TrackEventColor },
			{ LevelEventType.AnimateTrack, TrackEventColor },
			{ LevelEventType.RecolorTrack, TrackEventColor },
			{ LevelEventType.MoveTrack, TrackEventColor },
			{ LevelEventType.PositionTrack, TrackEventColor },
			{ LevelEventType.Hide, TrackEventColor },
			{ LevelEventType.AddDecoration, DecorationEventColor },
			{ LevelEventType.AddText, DecorationEventColor },
			{ LevelEventType.AddObject, DecorationEventColor },
			{ LevelEventType.AddParticle, DecorationEventColor },
			{ LevelEventType.MoveDecorations, DecorationEventColor },
			{ LevelEventType.SetText, DecorationEventColor },
			{ LevelEventType.SetObject, DecorationEventColor },
			{ LevelEventType.SetParticle, DecorationEventColor },
			{ LevelEventType.EmitParticle, DecorationEventColor },
			{ LevelEventType.SetDefaultText, DecorationEventColor },
			{ LevelEventType.MoveCamera, CameraEventColor },
			{ LevelEventType.ShakeScreen, CameraEventColor },
			{ LevelEventType.ScreenTile, CameraEventColor },
			{ LevelEventType.ScreenScroll, CameraEventColor },
			{ LevelEventType.CustomBackground, FilterEventColor },
			{ LevelEventType.Flash, FilterEventColor },
			{ LevelEventType.SetFilter, FilterEventColor },
			{ LevelEventType.SetFilterAdvanced, FilterEventColor },
			{ LevelEventType.HallOfMirrors, FilterEventColor },
			{ LevelEventType.Bloom, FilterEventColor },
			{ LevelEventType.SetFrameRate, FilterEventColor },
			{ LevelEventType.RepeatEvents, ModifierEventColor },
			{ LevelEventType.SetConditionalEvents, ModifierEventColor },
			{ LevelEventType.SetInputEvent, ModifierEventColor },
			{ LevelEventType.EditorComment, CommentEventColor },
			{ LevelEventType.Bookmark, CommentEventColor }
		};
	}
}
