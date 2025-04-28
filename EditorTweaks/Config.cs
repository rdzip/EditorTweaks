using UnityModManagerNet;

namespace EditorTweaks
{
	public class Config : UnityModManager.ModSettings
	{
		public bool horizontalProperty = false;
		public bool instantApplyColor = false;

		public bool timelineHorizontalScrollDirectionInvert = false;
		public bool timelineJumpToFloor = true;
		public bool timelineJumpToEvent = true;
	}
}
