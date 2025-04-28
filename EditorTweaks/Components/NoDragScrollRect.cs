using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EditorTweaks.Components
{
    public class NoDragScrollRect : ScrollRect
    {
		public Action<PointerEventData> onBeginDrag;
		public Action<PointerEventData> onDrag;
		public Action<PointerEventData> onEndDrag;

		public override void OnBeginDrag(PointerEventData eventData)
		{
			onBeginDrag?.Invoke(eventData);
		}
		public override void OnDrag(PointerEventData eventData)
		{
			onDrag?.Invoke(eventData);
		}
		public override void OnEndDrag(PointerEventData eventData)
		{
			onEndDrag?.Invoke(eventData);
		}
	}
}
