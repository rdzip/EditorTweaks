using ADOFAI;
using EditorTweaks.Utils;
using UnityEngine;

namespace EditorTweaks.Patch.Timeline
{
	public class LevelEventData
	{
		public float start => (float)scnEditor.instance.floors[evt.floor].entryTime;
		public float duration => ETUtils.GetEventDurationWithFloorSpeed(evt);
		public float end => start + duration;

		public readonly LevelEvent evt;

		private int _timelineRowNumber;
		public int timelineRowNumber
		{
			get => _timelineRowNumber;
			set
			{
				ETUtils.SetRowNumber(evt, value);
				_timelineRowNumber = value;
			}
		}

		public TimelineEvent obj
		{
			get
			{
				if (_obj == null) return null;
				if (_obj.targetEvent != evt)
					return null;
				return _obj;
			}
			set
			{
				_obj = value;
			}
		}
		private TimelineEvent _obj;

		public LevelEventData(int timelineRow, LevelEvent evt)
		{
			this.evt = evt;
			//this.timelineRow = timelineRow;
			_timelineRowNumber = timelineRow;
		}
	}
}
