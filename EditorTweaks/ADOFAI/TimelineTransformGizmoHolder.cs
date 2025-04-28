using ADOFAI;
using DG.Tweening;
using EditorTweaks.Patch.Timeline;
using EditorTweaks.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTweaks.ADOFAI
{
	public class TimelineTransformGizmoHolder : TransformGizmoHolder
	{
		public int minSize = 300;
		public int maxSize = 600;
		private RectTransform rect;
		private TimelinePanel panel;
		private bool dragging;
		private Vector2 startPanelSize;

		private float Validate(float v)
		{
			float num = Mathf.Clamp(v, minSize, maxSize);
			//if (this.snapTarget != null)
			//{
			//	float x = this.snapTarget.sizeDelta.x;
			//	if (Mathf.Abs(x - num) <= (float)this.snapRange)
			//	{
			//		num = x;
			//	}
			//}
			return num;
		}

		private new void Awake()
		{
			forUI = true;

			transform.GetChild(1).gameObject.SetActive(true);
			handles = new List<Handle>()
			{ 
				new Handle()
				{
					gizmoRect = (RectTransform)transform.GetChild(1),
					imageRect = (RectTransform)transform.GetChild(1).GetChild(0),
					transformGizmo = transform.GetChild(1).gameObject.GetComponent<TransformGizmo>()
				}
			};
			base.Awake();
			rect = GetComponent<RectTransform>();
			panel = GetComponentInParent<TimelinePanel>();
			//panelParentRect = panel.transform.parent.GetComponent<RectTransform>();
			//float f = Persistence.generalPrefs.GetFloat(this.xKey, 0f);
			//((RectTransform)transform).SizeDeltaY(Validate(f));
		}

		private void Update()
		{
			if (!isEditingLevel)
			{
				return;
			}
			if (transform.localScale != Vector3.one)
			{
				ETLogger.Error("localScale is different than one! " + transform.localScale.ToString());
				transform.localScale = Vector3.one;
			}
		}

		protected override void LateUpdate()
		{
			if (!isEditingLevel)
			{
				if (!dragging)
				{
					return;
				}
				editor.draggingGizmo = null;
				editor.lastHoveredGizmo = null;
				DragEnd();
			}
			UpdateGizmosTransform();
			UpdateGizmosVisibility();
		}

		public override void Drag(Vector2 mouseTranslation, Vector2 mouseDelta)
		{
			Vector2 vector;
			RectTransform panelRect = (RectTransform)panel.transform;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, mouseDelta, null, out vector);
			//if (Mathf.Approximately(panelRect.anchorMin.y, 1f))
			//{
			//	y = -(y + panelParentRect.rect.height);
			//}
			panelRect.SizeDeltaY(Validate(startPanelSize.y + vector.y));
			panel.Resize();

			scnEditor editor = scnEditor.instance;
			InspectorPanel settingsPanel = editor.settingsPanel;
			InspectorPanel levelEventsPanel = editor.levelEventsPanel;
			RectTransform bottomPanel = (RectTransform)editor.levelEditorCanvas.transform.Find("bottomPanel");

			float timelineHeight = Validate(startPanelSize.y + vector.y);
			float targetHeight = timelineHeight + 135;

			settingsPanel.rect.offsetMin = settingsPanel.rect.offsetMin.WithY(targetHeight);
			{
				List<string> list2 = GCS.settingsInfo.Keys.ToList();
				int count = list2.Count;
				float height = settingsPanel.tabs.rect.height;
				float num = 68f;
				if ((float)count * 68f >= height)
				{
					float num2 = (height - 68f * (float)count) / (float)(count * count);
					num = height / (float)count + num2;
				}
				int num3 = -1;
				foreach (RectTransform RT in settingsPanel.tabs)
				{
					bool flag2 = list2.Contains(RT.name);
					RT.gameObject.SetActive(flag2);
					if (flag2)
					{
						num3++;
						list2.Remove(RT.name);
					}
					float num4 = -num * (float)num3;
					RT.GetComponent<RectTransform>().SetAnchorPosY(num4);
				}
			}

			levelEventsPanel.rect.offsetMin = levelEventsPanel.rect.offsetMin.WithY(targetHeight);
			{
				if (scnEditor.instance.selectedFloors.Count != 0 && scnEditor.instance.selectedFloors.Count <= 1)
				{
					HashSet<LevelEventType> list = new HashSet<LevelEventType>();
					foreach (LevelEvent levelEvent in scnEditor.instance.events)
					{
						if (levelEvent.floor == scnEditor.instance.selectedFloors[0].seqID)
						{
							list.Add(levelEvent.eventType);
						}
					}

					//List<string> list2 = GCS.settingsInfo.Keys.ToList();
					int count = list.Count;
					float height = levelEventsPanel.tabs.rect.height;
					float num = 68f;
					if ((float)count * 68f >= height)
					{
						float num2 = (height - 68f * (float)count) / (float)(count * count);
						num = height / (float)count + num2;
					}
					int num3 = -1;
					foreach (RectTransform RT in levelEventsPanel.tabs)
					{
						var enumerator = list.Where((t) => t.ToString() == RT.name);
						bool flag2 = enumerator.Any();
						RT.gameObject.SetActive(flag2);
						if (flag2)
						{
							LevelEventType eventType = enumerator.First();
							num3++;
							list.Remove(eventType);
						}
						float num4 = -num * (float)num3;
						RT.GetComponent<RectTransform>().SetAnchorPosY(num4);
					}
				}
			}
			bottomPanel.LocalMoveY(timelineHeight - 675);
			float[] offsets = new float[] { 667, 659, 659, 575 };
			for (int i = 0; i < editor.ottoCanvas.transform.childCount; i++)
			{
				Transform child = editor.ottoCanvas.transform.GetChild(i);
				child.LocalMoveY(timelineHeight - offsets[i]);
			}
		}

		public override void DragEnd()
		{
			dragging = false;
			//if (!string.IsNullOrEmpty(this.saveName))
			//{
			//	Persistence.generalPrefs.SetFloat(this.xKey, this.panel.rect.sizeDelta.x);
			//}
		}

		public override void DragStart(TransformGizmo handle)
		{
			dragging = true;
			startPanelSize = ((RectTransform)panel.transform).sizeDelta;
		}

		private void UpdateGizmosTransform()
		{
			Handle handle = handles[0];
			Vector3 mousePosition = Input.mousePosition;
			Vector2 vector;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(handle.gizmoRect, mousePosition, null, out vector);
			handle.imageRect.anchoredPosition = handle.imageRect.anchoredPosition.WithX(vector.x);
			if (!dragging)
			{
				HandleAnimation(handle);
			}
			if (handle.collider != null)
			{
				handle.collider.size = handle.collider.size.WithX(rect.rect.width);
			}
		}
	}
}
