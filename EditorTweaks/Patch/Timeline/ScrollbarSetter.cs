using UnityEngine;

namespace EditorTweaks.Patch.Timeline
{
	public class ScrollbarSetter : MonoBehaviour
	{
		public RectTransform horizontal;
		public RectTransform vertical;

		void LateUpdate()
		{
			horizontal.offsetMax = new Vector2(
				vertical.gameObject.activeSelf ? -vertical.sizeDelta.x : 0,
				horizontal.offsetMax.y
			);
			vertical.offsetMin = new Vector2(
				vertical.offsetMin.x,
				horizontal.gameObject.activeSelf ? horizontal.sizeDelta.y : 0
			);
		}
	}
}
