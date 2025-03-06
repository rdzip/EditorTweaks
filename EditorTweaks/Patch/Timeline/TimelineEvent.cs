using ADOFAI;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTweaks.Patch.Timeline
{
	public class TimelineEvent : MonoBehaviour
	{
		public TimelinePanel panel;
		public Button button;

		public LevelEvent targetEvent;

		void Start()
		{
			button.onClick.AddListener(() =>
			{
				scnEditor editor = scnEditor.instance;
				editor.SelectFloor(editor.floors[targetEvent.floor]);
				editor.levelEventsPanel.ShowPanelOfEvent(targetEvent);
			});
		}

		void Update() { }

		//public void UnselectEvent()
		//{
		//	button.interactable = true;
		//}
	}
}
