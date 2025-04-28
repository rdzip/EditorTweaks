using ADOFAI;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTweaks.Patch.Timeline
{
	public class TimelineEvent : MonoBehaviour
	{
		public TimelinePanel panel;
		public Image overlayImage;
		public Image offImage;
		public Button button;

		public LevelEvent targetEvent;

		void Start()
		{
			button.onClick.AddListener(() =>
			{
				if (RDInput.holdingControl)
					panel.ToggleActiveEvent(targetEvent);
				else
					panel.ToggleSelectEvent(targetEvent, RDInput.holdingControl || RDInput.holdingShift);
			});
		}

		void Update() { }

		//public void UnselectEvent()
		//{
		//	button.interactable = true;
		//}
	}
}
