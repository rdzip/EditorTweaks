using EditorTweaks.Utils;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using ADOFAI;
using DG.Tweening;
using ADOFAI.LevelEditor.Controls;

namespace EditorTweaks.Patch.Timeline
{
	[HarmonyPatch]
	public static class TimelinePatch
	{
		private static TimelinePanel timeline;

		private static GameObject verticalLineObj;
		private static FloorNumberLine verticalLine;
		private static GameObject horizontalLineObj;
		private static GameObject eventObj;

		private static bool showingTimeline = false;

		private static Tween settingsPanelTween;
		private static Tween levelEventsPanelTween;
		private static Tween bottomPanelTween;
		private static Tween timelinePanelTween;
		private static Tween[] ottoCanvasTween;

		[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.LoadEditorProperties))]
		[HarmonyPostfix]
		public static void CreateTimeline(scnEditor __instance)
		{
			Canvas canvas = __instance.levelEditorCanvas;
			RectTransform levelEventsBar = __instance.levelEventsBar;

			GameObject timelineObj = new GameObject("timelinePanel");
			timelineObj.transform.SetParent(canvas.transform, false);
			RectTransform timelineRT = timelineObj.AddComponent<RectTransform>();
			timelineRT.offsetMin = new Vector2(150f, 0f);
			timelineRT.offsetMax = new Vector2(-150f, 0f);
			timelineRT.anchorMin = Vector2.zero;
			timelineRT.anchorMax = Vector2.right;
			timelineRT.pivot = new Vector2(0.5f, 0f);
			timelineRT.SizeDeltaY(300f);
			timelineRT.localPosition = Vector3.zero;
			timelineRT.anchoredPosition = Vector2.zero;

			Image backgroundPrefab = levelEventsBar.Find("background").GetComponent<Image>();
			Image background = GameObject.Instantiate(backgroundPrefab, timelineObj.transform);

			ScrollRect scrollRect = ETUtils.CreateScrollRect(timelineRT, true, true);
			ScrollbarSetter scrollbarSetter = scrollRect.gameObject.AddComponent<ScrollbarSetter>();
			scrollbarSetter.horizontal = (RectTransform)scrollRect.horizontalScrollbar.transform;
			scrollbarSetter.vertical = (RectTransform)scrollRect.verticalScrollbar.transform;

			RectTransform contentRT = scrollRect.content;

			GameObject gridObj = new GameObject("grid");
			gridObj.transform.SetParent(contentRT.transform, false);
			RectTransform gridRT = gridObj.AddComponent<RectTransform>();
			gridRT.offsetMin = Vector2.zero;
			gridRT.offsetMax = Vector2.zero;
			gridRT.anchorMin = Vector2.zero;
			gridRT.anchorMax = Vector2.one;
			gridRT.pivot = new Vector2(0f, 0.5f);
			gridRT.localPosition = Vector3.zero;
			gridRT.anchoredPosition = Vector2.zero;

			GameObject eventsObj = new GameObject("events");
			eventsObj.transform.SetParent(contentRT.transform, false);
			RectTransform eventsRT = eventsObj.AddComponent<RectTransform>();
			eventsRT.offsetMin = new Vector2(0f, 25f);
			eventsRT.offsetMax = Vector2.zero;
			eventsRT.anchorMin = Vector2.zero;
			eventsRT.anchorMax = Vector2.one;
			eventsRT.pivot = Vector2.up;
			eventsRT.localPosition = Vector3.zero;
			eventsRT.anchoredPosition = new Vector2(0f, -25f);

			GameObject parentObject = ETUtils.FindOrCreatePrefabsObject();

			if (verticalLineObj == null)
			{
				verticalLineObj = new GameObject("verticalLine");
				verticalLineObj.transform.SetParent(parentObject.transform, false);

				RectTransform verticalLineRT = verticalLineObj.AddComponent<RectTransform>();
				verticalLineRT.offsetMin = Vector2.zero;
				verticalLineRT.offsetMax = Vector2.zero;
				verticalLineRT.anchorMin = Vector2.zero;
				verticalLineRT.anchorMax = Vector2.up;
				verticalLineRT.SizeDeltaX(2f);
				verticalLineRT.localPosition = Vector3.zero;
				verticalLineRT.anchoredPosition = Vector2.zero;

				verticalLineObj.AddComponent<Image>().color = Color.white.WithAlpha(0.5f);

				GameObject floorTextObj = new GameObject("text");
				floorTextObj.transform.SetParent(verticalLineRT, false);

				RectTransform floorTextRT = floorTextObj.AddComponent<RectTransform>();
				floorTextRT.anchorMin = Vector2.up;
				floorTextRT.anchorMax = Vector2.up;
				floorTextRT.pivot = Vector2.up;
				floorTextRT.SizeDeltaY(25f);
				floorTextRT.localPosition = new Vector2(6f, 0f);

				floorTextObj.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

				TextMeshProUGUI floorText = floorTextObj.AddComponent<TextMeshProUGUI>();
				floorText.alignment = TextAlignmentOptions.Left;
				floorText.color = Color.white;
				floorText.font = RDString.editorFonts[0];
				floorText.fontSize = 12f;

				verticalLine = verticalLineObj.AddComponent<FloorNumberLine>();
				verticalLine.vLine = verticalLineObj;
				verticalLine.text = floorText;
			}
			if (horizontalLineObj == null)
			{
				horizontalLineObj = new GameObject("horizontalLine");
				horizontalLineObj.transform.SetParent(parentObject.transform, false);

				RectTransform horizontalLineRT = horizontalLineObj.AddComponent<RectTransform>();
				horizontalLineRT.offsetMin = Vector2.zero;
				horizontalLineRT.offsetMax = Vector2.zero;
				horizontalLineRT.anchorMin = Vector2.zero;
				horizontalLineRT.anchorMax = Vector2.right;
				horizontalLineRT.SizeDeltaY(2f);
				horizontalLineRT.localPosition = Vector3.zero;
				horizontalLineRT.anchoredPosition = Vector2.zero;

				horizontalLineObj.AddComponent<Image>().color = Color.white.WithAlpha(0.5f);
			}
			if (eventObj == null)
			{
				eventObj = new GameObject("event");
				eventObj.transform.SetParent(parentObject.transform, false);

				RectTransform eventRT = eventObj.AddComponent<RectTransform>();
				eventRT.anchorMin = Vector2.up;
				eventRT.anchorMax = Vector2.up;
				eventRT.pivot = Vector2.up;
				eventRT.sizeDelta = new Vector2(100f, 30f);
				eventRT.localPosition = new Vector2(0f, 0f);

				Image eventImage = eventObj.AddComponent<Image>();
				eventImage.sprite = ETUtils.FindUISprite();
				eventImage.type = Image.Type.Sliced;
				eventImage.fillCenter = true;
				eventImage.pixelsPerUnitMultiplier = 1f;

				Button eventButton = eventObj.AddComponent<Button>();
				eventButton.targetGraphic = eventImage;

				eventObj.AddComponent<TimelineEvent>().button = eventButton;

				GameObject eventIconObj = new GameObject("icon");
				eventIconObj.transform.SetParent(eventRT, false);

				RectTransform eventIconRT = eventIconObj.AddComponent<RectTransform>();
				eventIconRT.anchorMin = new Vector2(0f, 0.5f);
				eventIconRT.anchorMax = new Vector2(0f, 0.5f);
				eventIconRT.pivot = new Vector2(0.5f, 0.5f);
				eventIconRT.sizeDelta = new Vector2(25f, 25f);
				eventIconRT.anchoredPosition = Vector2.zero;
				eventIconRT.localPosition = new Vector2(15f, -15f);

				Image iconImage = eventIconObj.AddComponent<Image>();
				iconImage.color = Color.white;
			}

			timeline = timelineObj.AddComponent<TimelinePanel>();

			timeline.horizontalLine = horizontalLineObj;
			timeline.verticalLine = verticalLine;
			timeline.eventObj = eventObj;

			timeline.content = contentRT;
			timeline.grid = gridRT;
			timeline.events = eventsRT;
			timeline.scroll = scrollRect;
			timeline.scrollRT = scrollRect.GetComponent<RectTransform>();

			GameObject timelineButton = GameObject.Instantiate(__instance.prefab_eventCategoryTab, timelineRT);
			timelineButton.transform.LocalMoveX(timelineButton.transform.localPosition.x + 17f);
			CategoryTab buttonComponent = timelineButton.GetComponent<CategoryTab>();
			buttonComponent.icon.sprite = ETUtils.LoadTimelineIcon();
			buttonComponent.button.onClick.RemoveAllListeners();
			buttonComponent.button.onClick.AddListener(() => ToggleTimeline());
			buttonComponent.enabled = false;

			showingTimeline = false;

			timelineRT.LocalMoveY(timelineRT.localPosition.y - 300f);
		}

		public static void ToggleTimeline()
		{
			ShowTimeline(!showingTimeline);
		}

		public static void ShowTimeline(bool show = true)
		{
			if (show == showingTimeline) return;

			scnEditor editor = scnEditor.instance;
			InspectorPanel settingsPanel = editor.settingsPanel;
			InspectorPanel levelEventsPanel = editor.levelEventsPanel;
			RectTransform bottomPanel = (RectTransform)editor.levelEditorCanvas.transform.Find("bottomPanel");
			RectTransform timelinePanel = (RectTransform)timeline.transform;

			if (ottoCanvasTween == null)
				ottoCanvasTween = new Tween[editor.ottoCanvas.transform.childCount];

			settingsPanelTween?.Kill(true);
			levelEventsPanelTween?.Kill(true);
			bottomPanelTween?.Kill(true);
			timelinePanelTween?.Kill(true);
			foreach (var tween in ottoCanvasTween)
			{
				tween?.Kill(true);
			}

			if (show)
			{
				settingsPanelTween =
					DOTween.To(() => settingsPanel.rect.offsetMin, (v) => settingsPanel.rect.offsetMin = v,
					settingsPanel.rect.offsetMin.WithY(settingsPanel.rect.offsetMin.y + 300f), editor.UIPanelEaseDur)
					.SetEase(editor.UIPanelEaseMode).SetUpdate(true).OnUpdate(() =>
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
					});
				levelEventsPanelTween =
					DOTween.To(() => levelEventsPanel.rect.offsetMin, (v) => levelEventsPanel.rect.offsetMin = v,
					levelEventsPanel.rect.offsetMin.WithY(levelEventsPanel.rect.offsetMin.y + 300f), editor.UIPanelEaseDur)
					.SetEase(editor.UIPanelEaseMode).SetUpdate(true).OnUpdate(() =>
					{
						if (scnEditor.instance.selectedFloors.Count == 0 || scnEditor.instance.selectedFloors.Count > 1) return;
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
					});
				bottomPanelTween =
					bottomPanel.DOLocalMoveY(bottomPanel.localPosition.y + 300f, editor.UIPanelEaseDur)
					.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
				timelinePanelTween =
					timelinePanel.DOLocalMoveY(timelinePanel.localPosition.y + 300f, editor.UIPanelEaseDur)
					.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
				for (int i = 0; i < editor.ottoCanvas.transform.childCount; i++)
				{
					Transform child = editor.ottoCanvas.transform.GetChild(i);
					ottoCanvasTween[i] = child.DOLocalMoveY(child.localPosition.y + 300f, editor.UIPanelEaseDur)
					.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
				}
			}
			else
			{
				settingsPanelTween =
					DOTween.To(() => settingsPanel.rect.offsetMin, (v) => settingsPanel.rect.offsetMin = v,
					settingsPanel.rect.offsetMin.WithY(settingsPanel.rect.offsetMin.y - 300f), editor.UIPanelEaseDur)
					.SetEase(editor.UIPanelEaseMode).SetUpdate(true).OnUpdate(() =>
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
					});
				levelEventsPanelTween =
					DOTween.To(() => levelEventsPanel.rect.offsetMin, (v) => levelEventsPanel.rect.offsetMin = v,
					levelEventsPanel.rect.offsetMin.WithY(levelEventsPanel.rect.offsetMin.y - 300f), editor.UIPanelEaseDur)
					.SetEase(editor.UIPanelEaseMode).SetUpdate(true).OnUpdate(() =>
					{
						if (scnEditor.instance.selectedFloors.Count == 0 || scnEditor.instance.selectedFloors.Count > 1) return;
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
					});
				bottomPanelTween =
					bottomPanel.DOLocalMoveY(bottomPanel.localPosition.y - 300f, editor.UIPanelEaseDur)
					.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
				timelinePanelTween =
					timelinePanel.DOLocalMoveY(timelinePanel.localPosition.y - 300f, editor.UIPanelEaseDur)
					.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
				for (int i = 0; i < editor.ottoCanvas.transform.childCount; i++)
				{
					Transform child = editor.ottoCanvas.transform.GetChild(i);
					ottoCanvasTween[i] = child.DOLocalMoveY(child.localPosition.y - 300f, editor.UIPanelEaseDur)
					.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
				}
			}
			showingTimeline = show;
		}

		[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.CloseAllInspectors))]
		[HarmonyPostfix]
		public static void HideTimeline()
		{
			ShowTimeline(false);
		}

		[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.NewLevel))]
		[HarmonyPostfix]
		public static void InitOnNewLevel()
		{
			timeline.Init();
		}

		//[HarmonyPatch(typeof(scnEditor), "OpenLevelCo")]
		//[HarmonyPostfix]
		//public static IEnumerator InitOnOpenLevel(IEnumerator __result)
		//{
		//	while (__result.MoveNext())
		//	{
		//		yield return __result.Current;
		//	}
		//	timeline.Init();
		//}

		[HarmonyPatch(typeof(scnEditor), "Start")]
		[HarmonyPostfix]
		public static void InitOnStart()
		{
			timeline.Init();
		}

		[HarmonyPatch(typeof(scnEditor), "ShowEventPicker")]
		[HarmonyPrefix]
		public static bool ShowEventPickerPatch(scnEditor __instance, bool show)
		{
			__instance.levelEventsBar.DOScaleY(show ? 1f : 0f, 0.25f).SetUpdate(true).SetEase(__instance.UIPanelEaseMode);
			//__instance.levelEventsBar.DOLocalMoveY(show ? 0f : 0f, 0.25f).SetUpdate(true).SetEase(__instance.UIPanelEaseMode);
			foreach (CategoryTab categoryTab in __instance.Get<List<CategoryTab>>("categoryTabs"))
			{
				categoryTab.SetSelected(show && __instance.currentCategory == categoryTab.levelEventCategory);
			}
			if (show)
			{
				__instance.ShowEventsPage(__instance.currentPage);
				return false;
			}
			__instance.eventPickerText.text = "";
			__instance.categoryText.text = "";
			return false;
		}

		[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RemakePath))]
		[HarmonyPostfix]
		public static void ReloadTimelineAtRemakePath(bool applyEventsToFloors)
		{
			if (applyEventsToFloors)
				timeline.Init(timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
		}

		[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ApplyEventsToFloors))]
		[HarmonyPostfix]
		public static void ReloadTimelineAtApplyEvent()
		{
			timeline.Init(timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
		}

		[HarmonyPatch(typeof(PropertyControl_Text), nameof(PropertyControl_Text.Validate))]
		[HarmonyPostfix]
		public static void ReloadTimelineAtModifiedEvent(PropertyControl_Text __instance)
		{
			if (__instance.propertyInfo.name == "angleOffset" || __instance.propertyInfo.name == "duration")
				scnEditor.instance.StartCoroutine(ReloadAtNextFrame());
		}

		static IEnumerator ReloadAtNextFrame()
		{
			yield return null;
			timeline.Init(timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
		}

		[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.SelectFloor))]
		[HarmonyPostfix]
		public static void MoveTimelineToSelectedFloor(scrFloor floorToSelect)
		{
			timeline.MoveToFloor(floorToSelect);
		}
	}
}
