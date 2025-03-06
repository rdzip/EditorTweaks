using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace EditorTweaks.ADOFAI
{
	[RequireComponent(typeof(ScrollRect))]
	public class SmoothScrollRectForTimeline : MonoBehaviour, IScrollHandler, IEventSystemHandler
	{
		public new bool enabled = true;

		public ScrollRect scrollRect;

		public float scrollSensitivity = 1f;

		public float scrollSensitivityHorizontal = 1f;

		public float tweenDuration = 0.2f;

		public Ease ease = Ease.OutCubic;

		private const float scrollMultiplier = 1f;

		private RectTransform contentRect;

		private RectTransform viewportRect;

		private Vector2 targetPosition;

		private Tween currentTween;

		private MoveDirection lastDirection;

		private void Awake()
		{
			if (!scrollRect)
			{
				scrollRect = GetComponent<ScrollRect>();
			}

			scrollRect.onValueChanged.AddListener(OnScroll);
			contentRect = scrollRect.content;
			viewportRect = scrollRect.viewport;
			scrollRect.scrollSensitivity = 0f;
		}

		public void OnScroll(PointerEventData evt)
		{
			if (enabled)
			{
				bool tweening = currentTween != null;
				MoveDirection dir = RDInput.holdingShift ? (evt.scrollDelta.y > 0f ? MoveDirection.Up : MoveDirection.Down) : ((evt.scrollDelta.y * (Main.ETConfig.timelineHorizontalScrollDirectionInvert ? -1 : 1)) > 0f ? MoveDirection.Right : MoveDirection.Left);
				bool opposite = dir != lastDirection;
				lastDirection = dir;
				if ((RDInput.holdingShift && ((dir == MoveDirection.Up && scrollRect.verticalNormalizedPosition > 0f) || (dir == MoveDirection.Down && scrollRect.verticalNormalizedPosition < 1f)))
					|| (!RDInput.holdingShift && ((dir == MoveDirection.Left && scrollRect.horizontalNormalizedPosition > 0f) || (dir == MoveDirection.Right && scrollRect.horizontalNormalizedPosition < 1f))))
				{
					// Bad code x_x
					float scrollSensitivityH = scrollSensitivityHorizontal * (Main.ETConfig.timelineHorizontalScrollDirectionInvert ? -1 : 1);
					float num = evt.scrollDelta.y * (RDInput.holdingShift ? scrollSensitivity : scrollSensitivityH) * 1f * -1f;
					float x = Mathf.Clamp(((!opposite || !tweening) ? contentRect.anchoredPosition.x : targetPosition.x) + (!RDInput.holdingShift ? num : 0f), viewportRect.rect.width, contentRect.rect.width);
					float y = Mathf.Clamp(((!opposite || !tweening) ? contentRect.anchoredPosition.y : targetPosition.y) + (RDInput.holdingShift ? num : 0f), viewportRect.rect.height + 0.001f, contentRect.rect.height);
					targetPosition = new Vector2(x, y);
					ScrollTo(targetPosition);
				}
			}
		}

		public void ScrollTo(Vector2 position)
		{
			currentTween?.Kill();
			currentTween = contentRect.DOAnchorPos(position, tweenDuration).SetEase(ease).SetUpdate(isIndependentUpdate: true).OnComplete(() => currentTween = null);
		}

		private void OnScroll(Vector2 value)
		{
			if (enabled && Input.mouseScrollDelta.y == 0f && currentTween == null)
			{
				targetPosition = contentRect.anchoredPosition;
			}
		}
	}
}
