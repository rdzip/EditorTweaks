using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EditorTweaks.Components
{
	public class DragEventSender : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		public Action<PointerEventData> onBeginDrag;
		public Action<PointerEventData> onDrag;
		public Action<PointerEventData> onEndDrag;

		public void OnBeginDrag(PointerEventData eventData)
		{
			onBeginDrag?.Invoke(eventData);
		}

		public void OnDrag(PointerEventData eventData)
		{
			onDrag?.Invoke(eventData);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			onEndDrag?.Invoke(eventData);
		}
	}
}
