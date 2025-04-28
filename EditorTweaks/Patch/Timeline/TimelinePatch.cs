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
using System;
using System.Reflection;
using System.Diagnostics;
using SA.GoogleDoc;
using EditorTweaks.Components;
using ADOFAI.Editor;
using ADOFAI.Editor.Actions;
using OggVorbisEncoder.Setup;
using UnityEngine.SceneManagement;
using EditorTweaks.ADOFAI;

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

		private static bool undoOrRedoing = false;
		private static bool addingOrRemovingEvent = false;

		[HarmonyPatch]
		public static class TimelineCreatePatch
		{
			static Canvas timelineCanvas;
			static CanvasScaler timelineCanvasScaler;

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.LoadEditorProperties))]
			[HarmonyPostfix]
			public static void CreateTimeline(scnEditor __instance)
			{
				//Canvas canvas = __instance.levelEditorCanvas;
				RectTransform levelEventsBar = __instance.levelEventsBar;

				Scene currentScene = SceneManager.GetActiveScene();

				SceneManager.SetActiveScene(SceneManager.GetSceneByName("scnEditor"));

				timelineCanvas = new GameObject("timelineCanvas").AddComponent<Canvas>();
				timelineCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

				GraphicRaycaster raycaster = timelineCanvas.gameObject.AddComponent<GraphicRaycaster>();

				timelineCanvasScaler = timelineCanvas.gameObject.AddComponent<CanvasScaler>();
				timelineCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				timelineCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
				timelineCanvasScaler.matchWidthOrHeight = 1f;
				timelineCanvasScaler.referenceResolution = __instance.Get<CanvasScaler>("canvasScaler").referenceResolution;

				SceneManager.SetActiveScene(currentScene);

				GameObject timelineObj = new GameObject("timelinePanel");
				timelineObj.transform.SetParent(timelineCanvas.transform, false);
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
				background.material = __instance.fileActionsPanel.transform.GetChild(0).gameObject.GetComponent<Image>().material;

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

				GameObject selectionObj = new GameObject("selection");
				selectionObj.transform.SetParent(contentRT, false);
				RectTransform selectionRT = selectionObj.AddComponent<RectTransform>();
				selectionRT.pivot = Vector2.up;
				selectionRT.anchorMin = Vector2.up;
				selectionRT.anchorMax = Vector2.up;
				selectionRT.sizeDelta = new Vector2(100, 100);
				Image selectionImage = selectionObj.AddComponent<Image>();
				selectionImage.type = Image.Type.Sliced;
				selectionImage.pixelsPerUnitMultiplier = 0.75f;
				selectionImage.sprite = ETUtils.LoadSelectionRect();
				selectionImage.color = Color.white.WithAlpha(0.5f);
				selectionObj.SetActive(false);

				GameObject parentObject = ETUtils.FindOrCreatePrefabsObject();

				DragEventSender eventSender = null;

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

					GameObject eventOverlayObj = new GameObject("overlay");
					eventOverlayObj.transform.SetParent(eventRT, false);

					RectTransform eventOverlayRT = eventOverlayObj.AddComponent<RectTransform>();
					eventOverlayRT.offsetMin = Vector2.zero;
					eventOverlayRT.offsetMax = Vector2.zero;
					eventOverlayRT.anchorMin = Vector2.zero;
					eventOverlayRT.anchorMax = Vector2.one;
					eventOverlayRT.localPosition = Vector2.zero;
					eventOverlayRT.anchoredPosition = Vector2.zero;

					Image overlayImage = eventOverlayObj.AddComponent<Image>();
					overlayImage.sprite = ETUtils.FindUISprite();
					overlayImage.type = Image.Type.Sliced;
					overlayImage.fillCenter = true;
					overlayImage.pixelsPerUnitMultiplier = 1f;
					overlayImage.color = Color.white.WithAlpha(0.375f);
					overlayImage.raycastTarget = true;

					GameObject eventOffObj = new GameObject("off");
					eventOffObj.transform.SetParent(eventRT, false);

					RectTransform eventOffRT = eventOffObj.AddComponent<RectTransform>();
					eventOffRT.offsetMin = Vector2.zero;
					eventOffRT.offsetMax = Vector2.zero;
					eventOffRT.anchorMin = Vector2.zero;
					eventOffRT.anchorMax = Vector2.one;
					eventOffRT.localPosition = Vector2.zero;
					eventOffRT.anchoredPosition = Vector2.zero;

					Image offImage = eventOffObj.AddComponent<Image>();
					offImage.sprite = ETUtils.FindUISprite();
					offImage.type = Image.Type.Sliced;
					offImage.fillCenter = true;
					offImage.pixelsPerUnitMultiplier = 1f;
					offImage.color = Color.black.WithAlpha(0.5f);
					overlayImage.raycastTarget = false;

					Button eventButton = eventObj.AddComponent<Button>();
					eventButton.targetGraphic = eventImage;

					TimelineEvent timelineEvent = eventObj.AddComponent<TimelineEvent>();
					timelineEvent.button = eventButton;
					timelineEvent.overlayImage = overlayImage;
					timelineEvent.offImage = offImage;

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
					iconImage.raycastTarget = false;

					eventSender = eventOverlayObj.AddComponent<DragEventSender>();
				}

				timeline = timelineObj.AddComponent<TimelinePanel>();

				timeline.horizontalLine = horizontalLineObj;
				timeline.verticalLine = verticalLine;
				timeline.eventObj = eventObj;

				timeline.content = contentRT;
				timeline.grid = gridRT;
				timeline.events = eventsRT;
				timeline.scroll = scrollRect;
				timeline.selection = selectionRT;

				((NoDragScrollRect)scrollRect).onBeginDrag = timeline.OnBeginDrag;
				//((NoDragScrollRect)scrollRect).onDrag = timeline.OnDrag;
				((NoDragScrollRect)scrollRect).onEndDrag = timeline.OnEndDrag;

				if (eventSender != null)
				{
					eventSender.onBeginDrag = timeline.OnBeginDrag;
					//eventSender.onDrag = timeline.OnDrag;
					eventSender.onEndDrag = timeline.OnEndDrag;
				}

				GameObject transformGizmoHolder = GameObject.Instantiate(scnEditor.instance.transform.Find("settingsPanel").Find("panelTransformGizmoHolder").gameObject, timelineRT);
				transformGizmoHolder.GetComponent<PanelTransformGizmoHolder>().enabled = false;
				transformGizmoHolder.AddComponent<TimelineTransformGizmoHolder>();

				GameObject timelineButton = GameObject.Instantiate(__instance.prefab_eventCategoryTab, timelineRT);
				timelineButton.transform.LocalMoveX(timelineButton.transform.localPosition.x + 20f);
				CategoryTab buttonComponent = timelineButton.GetComponent<CategoryTab>();
				buttonComponent.icon.sprite = ETUtils.LoadTimelineIcon();
				buttonComponent.button.onClick.RemoveAllListeners();
				buttonComponent.button.onClick.AddListener(() => ToggleTimeline());
				buttonComponent.enabled = false;
				timelineButton.transform.GetChild(0).GetComponent<Image>().material = background.material;

				GameObject timelineControlsObj = GameObject.Instantiate(timelineButton, timelineRT);
				CategoryTab buttonComponent2 = timelineControlsObj.GetComponent<CategoryTab>();
				buttonComponent2.button.enabled = false;
				//buttonComponent2.icon.sprite = GCS.levelEventIcons[LevelEventType.RepeatEvents];
				buttonComponent2.enabled = false;
				Image timelineControlsBG = timelineControlsObj.transform.GetChild(0).GetComponent<Image>();
				timelineControlsBG.color = timelineControlsBG.color.WithAlpha(0.851f);

				RectTransform timelineControlsRT = timelineControlsObj.GetComponent<RectTransform>();
				//timelineControlsRT.offsetMin = timelineControlsRT.offsetMin.WithX(timelineControlsRT.sizeDelta.x * 4f - 160f);
				//timelineControlsRT.offsetMax = timelineControlsRT.offsetMax.WithX(-160f);
				timelineControlsRT.anchorMin = Vector2.one;
				timelineControlsRT.anchorMax = Vector2.one;
				timelineControlsRT.pivot = Vector2.right;
				timelineControlsRT.SizeDeltaX(timelineControlsRT.sizeDelta.x * 4f);
				timelineControlsRT.AnchorPosX(-160f);

				Image timelineReorderImage = buttonComponent2.icon;
				timelineReorderImage.rectTransform.LocalMoveX(buttonComponent.icon.rectTransform.localPosition.x);
				timelineReorderImage.raycastTarget = true;
				timelineReorderImage.sprite = GCS.levelEventIcons[LevelEventType.RepeatEvents];
				Button timelineReorderButton = timelineReorderImage.gameObject.AddComponent<Button>();
				timelineReorderButton.targetGraphic = timelineReorderImage;
				timelineReorderButton.onClick.AddListener(() =>
				{
					scrSfx.instance.PlaySfx(SfxSound.MobileButton, MixerGroup.InterfaceParent, 1f, 1f, 0f);
					if (!scrController.instance.paused) return;
					scnEditor.instance.ConfirmPopup(RDString.Get("editortweaks.reorderWarning"), () =>
					{
						using (new SaveStateScope(scnEditor.instance, false, true, false))
						{
							timeline.Init(true, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
						}
					});
				});

				GameObject timelineMagnetObj = new GameObject("magnetButtonContainer");
				timelineMagnetObj.transform.SetParent(timelineControlsRT, false);
				RectTransform timelineMagnetRT = timelineMagnetObj.AddComponent<RectTransform>();
				timelineMagnetRT.SizeDeltaX(timelineControlsRT.sizeDelta.x - ((timelineReorderImage.rectTransform.localPosition.x + timelineReorderImage.rectTransform.sizeDelta.x) * 2));
				timelineMagnetRT.SizeDeltaY(timelineReorderImage.rectTransform.sizeDelta.y);
				timelineMagnetObj.AddComponent<Image>().color = Color.white.WithAlpha(0);

				Image timelineMagnetImage = GameObject.Instantiate(timelineReorderImage, timelineMagnetObj.transform);
				timelineMagnetImage.rectTransform.LocalMoveX(-(timelineMagnetRT.sizeDelta.x / 2) + (timelineMagnetImage.rectTransform.sizeDelta.x / 2));
				timelineMagnetImage.sprite = ETUtils.LoadMagnetIcon();
				timelineMagnetImage.color = timelineMagnetImage.color.WithAlpha(1f);
				timelineMagnetImage.gameObject.GetComponent<Button>().enabled = false;

				GameObject magnetLeftTextObj = new GameObject("text");
				magnetLeftTextObj.transform.SetParent(timelineMagnetObj.transform, false);

				RectTransform magnetLeftTextRT = magnetLeftTextObj.AddComponent<RectTransform>();
				magnetLeftTextRT.SizeDeltaY(timelineMagnetImage.rectTransform.sizeDelta.y);
				magnetLeftTextRT.LocalMoveX(-8f);

				TextMeshProUGUI magnetLeftText = magnetLeftTextObj.AddComponent<TextMeshProUGUI>();
				magnetLeftText.alignment = TextAlignmentOptions.CaplineRight;
				magnetLeftText.color = Color.white.WithAlpha(0.8196f);
				magnetLeftText.font = RDString.editorFonts[0];
				magnetLeftText.fontSize = 20f;
				magnetLeftText.text = "1    /";

				magnetLeftText.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

				TextMeshProUGUI magnetRightText = GameObject.Instantiate(magnetLeftText, magnetLeftTextRT.parent);
				magnetRightText.rectTransform.LocalMoveX(28f);
				magnetRightText.alignment = TextAlignmentOptions.CaplineLeft;
				magnetRightText.text = "    " + timeline.magnetNum;

				GameObject magnetInputObj = GameObject.Instantiate(RDConstants.data.prefab_controlText, magnetLeftTextRT.parent);
				magnetInputObj.GetComponent<PropertyControl_Text>().enabled = false;
				magnetInputObj.GetComponent<LayoutElement>().enabled = false;
				magnetInputObj.GetComponent<Image>().color = Color.white.WithAlpha(0.8196f);

				RectTransform magnetInputRT = magnetInputObj.GetComponent<RectTransform>();
				magnetInputRT.sizeDelta = new Vector2(38f, timelineMagnetRT.sizeDelta.y);
				magnetInputRT.pivot = new Vector2(0.5f, 0.5f);
				magnetInputRT.LocalMoveX(38f);

				TMP_InputField magnetInput = magnetInputObj.GetComponent<TMP_InputField>();
				magnetInput.text = "64";
				magnetInput.onEndEdit.RemoveAllListeners();
				magnetInput.onEndEdit.AddListener((s) =>
				{
					try
					{
						int num = Convert.ToInt32(s);
						timeline.SetMagnet(num);
					}
					catch
					{
						magnetInput.text = "64";
						timeline.SetMagnet(64);
					}
				});

				Button timelineMangetButton = timelineMagnetObj.AddComponent<Button>();
				timelineMangetButton.targetGraphic = timelineMagnetImage;
				timelineMangetButton.onClick.RemoveAllListeners();
				timelineMangetButton.onClick.AddListener(() =>
				{
					scrSfx.instance.PlaySfx(SfxSound.MobileButton, MixerGroup.InterfaceParent, 1f, 1f, 0f);
					int num = timeline.SetMagnet();
					if (num < 0)
					{
						magnetRightText.gameObject.SetActive(false);
						magnetInput.gameObject.SetActive(true);
					}
					else
					{
						magnetRightText.gameObject.SetActive(true);
						magnetInput.gameObject.SetActive(false);
						magnetRightText.text = "    " + num;
					}
				});

				magnetInputObj.SetActive(false);

				Image timelinePlayheadImage = GameObject.Instantiate(timelineReorderImage, timelineReorderImage.transform.parent);
				timelinePlayheadImage.rectTransform.LocalMoveX(timelineControlsRT.sizeDelta.x - timelineReorderImage.rectTransform.localPosition.x);
				timelinePlayheadImage.sprite = ETUtils.LoadPlayheadIcon();
				timelinePlayheadImage.color = timeline.followPlayhead ? scnEditor.instance.editingColor.WithAlpha(0.8196f) : Color.white.WithAlpha(0.8196f);
				Button timelinePlayheadButton = timelinePlayheadImage.gameObject.GetComponent<Button>();
				timelinePlayheadButton.onClick.RemoveAllListeners();
				timelinePlayheadButton.onClick.AddListener(() =>
				{
					scrSfx.instance.PlaySfx(SfxSound.MobileButton, MixerGroup.InterfaceParent, 1f, 1f, 0f);
					timeline.followPlayhead = !timeline.followPlayhead;
					timelinePlayheadImage.color = timeline.followPlayhead ? scnEditor.instance.editingColor.WithAlpha(0.8196f) : Color.white.WithAlpha(0.8196f);
				});

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

				float timelineHeight = timelinePanel.sizeDelta.y;

				if (show)
				{
					settingsPanelTween =
						DOTween.To(() => settingsPanel.rect.offsetMin, (v) => settingsPanel.rect.offsetMin = v,
						settingsPanel.rect.offsetMin.WithY(settingsPanel.rect.offsetMin.y + timelineHeight), editor.UIPanelEaseDur)
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
						levelEventsPanel.rect.offsetMin.WithY(levelEventsPanel.rect.offsetMin.y + timelineHeight), editor.UIPanelEaseDur)
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
						bottomPanel.DOLocalMoveY(bottomPanel.localPosition.y + timelineHeight, editor.UIPanelEaseDur)
						.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
					timelinePanelTween =
						timelinePanel.DOLocalMoveY(timelinePanel.localPosition.y + timelineHeight, editor.UIPanelEaseDur)
						.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
					for (int i = 0; i < editor.ottoCanvas.transform.childCount; i++)
					{
						Transform child = editor.ottoCanvas.transform.GetChild(i);
						ottoCanvasTween[i] = child.DOLocalMoveY(child.localPosition.y + timelineHeight, editor.UIPanelEaseDur)
						.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
					}
				}
				else
				{
					settingsPanelTween =
						DOTween.To(() => settingsPanel.rect.offsetMin, (v) => settingsPanel.rect.offsetMin = v,
						settingsPanel.rect.offsetMin.WithY(settingsPanel.rect.offsetMin.y - timelineHeight), editor.UIPanelEaseDur)
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
						levelEventsPanel.rect.offsetMin.WithY(levelEventsPanel.rect.offsetMin.y - timelineHeight), editor.UIPanelEaseDur)
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
						bottomPanel.DOLocalMoveY(bottomPanel.localPosition.y - timelineHeight, editor.UIPanelEaseDur)
						.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
					timelinePanelTween =
						timelinePanel.DOLocalMoveY(timelinePanel.localPosition.y - timelineHeight, editor.UIPanelEaseDur)
						.SetEase(editor.UIPanelEaseMode).SetUpdate(true);
					for (int i = 0; i < editor.ottoCanvas.transform.childCount; i++)
					{
						Transform child = editor.ottoCanvas.transform.GetChild(i);
						ottoCanvasTween[i] = child.DOLocalMoveY(child.localPosition.y - timelineHeight, editor.UIPanelEaseDur)
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

			[HarmonyPatch(typeof(scnEditor), "UpdateCanvasScalerResolution")]
			[HarmonyPostfix]
			public static void UpdateTimelineCanvasScale(float height)
			{
				height = Mathf.Clamp(height, 900f, (float)(Screen.height * 2));
				float num = (float)Screen.width * 1f / (float)Screen.height * height;
				if (timelineCanvasScaler != null)
					timelineCanvasScaler.referenceResolution = new Vector2(num, height);
			}
		}

		[HarmonyPatch]
		public static class TimelineReloadPatch
		{
			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.NewLevel))]
			[HarmonyPostfix]
			public static void InitOnNewLevel()
			{
				timeline.Init();
				timeline.selectedEvents.Clear();
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ShowNotification))]
			[HarmonyPostfix]
			public static void InitOnOpenLevel(string text)
			{
				if (text == RDString.Get("editor.notification.levelLoaded", null, LangSection.Translations))
					timeline.Init(LevelEventPatch.shouldApply && LevelEventPatch.shouldOptimizeRows);
			}

			[HarmonyPatch(typeof(scnEditor), "Start")]
			[HarmonyPostfix]
			public static void InitOnStart()
			{
				if (string.IsNullOrEmpty(scnEditor.levelToOpenOnLoad))
					timeline.Init();
				//DebugPatch();
			}

			private static void DebugPatch()
			{
				var types = AppDomain.CurrentDomain.GetAssemblies()
					.Where(asm => asm.FullName == "EditorTweaks, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")
					.SelectMany(asm => asm.GetTypes())
					.Where(t => t.IsClass && t.Name == "TimelinePanel");
				Harmony harmony = new Harmony("EditorTweaks.Debug");

				foreach (var type in types)
				{
					foreach (var method in type.GetMethods())
					{
						//if (method.Name.Contains("DebugLog")) continue;
						//if (method.Name.Contains("OnEvent")) continue;
						//if (!method.Name.Contains("InitOn") && !method.Name.Contains("ReloadOn")) continue;
						try
						{
							harmony.Patch(method, prefix: typeof(TimelinePatch).GetMethod("DebugLog", AccessTools.all));
						}
						catch
						{
							ETLogger.Debug("DebugPatch failed: " + method.ReflectedType + "." + method.Name);
						}
					}
				}
			}

			private static void DebugLog()
			{
				StackTrace stackTrace = new StackTrace();
				StackFrame stackFrame = stackTrace.GetFrame(1);
				MethodBase methodBase = stackFrame.GetMethod();

				//ETLogger.Debug(methodBase.DeclaringType + "." + methodBase.Name);
				ETLogger.Debug(Environment.StackTrace);
				ETLogger.Debug("------------------------------------");
			}

			//[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RemakePath))]
			//[HarmonyPostfix]
			//public static void ReloadTimelineAtRemakePath(bool applyEventsToFloors)
			//{
			//	if (applyEventsToFloors)
			//	{
			//		bool optimizeRows = LevelEventPatch.shouldOptimalRows && LevelEventPatch.shouldApply;
			//		LevelEventPatch.shouldApply = false;
			//		timeline.Init(optimizeRows, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			//	}
			//}

			static bool handlingMouseActions = false;

			[HarmonyPatch(typeof(scnEditor), "HandleMouseActions")]
			[HarmonyPrefix]
			public static void PreHandleMouseActions()
			{
				handlingMouseActions = true;
			}

			[HarmonyPatch(typeof(scnEditor), "HandleMouseActions")]
			[HarmonyPostfix]
			public static void PostHandleMouseActions()
			{
				handlingMouseActions = false;
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RemakePath))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtRemakePath()
			{
				if (handlingMouseActions)
					timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			[HarmonyPatch(typeof(scnEditor), "DeleteFloor")]
			[HarmonyPostfix]
			public static void ReloadTimelineAtDeleteFloor()
			{
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.FlipFloor))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtFlipFloor()
			{
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.FlipSelection))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtFlipSelection()
			{
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.InsertCharFloor))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtInsertCharFloor()
			{
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.InsertFloatFloor))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtInsertFloatFloor()
			{
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.PasteFloors))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtPasteFloors()
			{
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RotateFloor))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtRotateFloor()
			{
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RotateFloor180))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtRotateFloor180()
			{
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RotateSelection))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtRotateSelection()
			{
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RotateSelection180))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtRotateSelection180()
			{
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}

			//[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ApplyEventsToFloors))]
			//[HarmonyPostfix]
			//public static void ReloadTimelineAtApplyEvent()
			//{
			//	timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			//}

			[HarmonyPatch(typeof(PropertyControl_Text), nameof(PropertyControl_Text.Validate))]
			[HarmonyPostfix]
			public static void ReloadTimelineAtModifiedEvent(PropertyControl_Text __instance)
			{
				if (__instance.propertyInfo.name == "angleOffset" || __instance.propertyInfo.name == "duration")
					scnEditor.instance.StartCoroutine(InvokeAtNextFrame(() =>
					{
						//__instance.propertiesPanel.inspectorPanel.selectedEvent;
						//timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
						timeline.levelEventDatas.Sort();

						timeline.Resize();

						timeline.Redraw();
					}));
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.UndoOrRedo))]
			[HarmonyPrefix]
			public static void PreUndoOrRedo()
			{
				undoOrRedoing = true;
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.UndoOrRedo))]
			[HarmonyPostfix]
			public static void PostUndoOrRedo()
			{
				undoOrRedoing = false;
				timeline.Init(false, timeline.scroll.normalizedPosition.x, timeline.scroll.normalizedPosition.y);
			}
		}

		[HarmonyPatch]
		public static class TimelineActionPatch
		{
			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.SelectFloor))]
			[HarmonyPostfix]
			public static void MoveTimelineToSelectedFloor(scrFloor floorToSelect)
			{
				if (scnEditor.instance.isLoading) return;
				timeline.MoveToFloor(floorToSelect, addingOrRemovingEvent && !undoOrRedoing);
			}

			[HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.ShowPanel))]
			[HarmonyPostfix]
			public static void MoveTimelineToSelectedEvent(InspectorPanel __instance)
			{
				if (__instance != scnEditor.instance.levelEventsPanel) return;
				if (scnEditor.instance.isLoading) return;
				if (timeline.selectedEvents.Count > 1) return;
				LevelEvent targetEvent = scnEditor.instance.levelEventsPanel.selectedEvent;
				if (targetEvent == null) return;
				if (targetEvent.IsDecoration || TimelineConstants.TimelineIgnoreEvents.Contains(targetEvent.eventType)) return;
				//timeline.ToggleSelectEvent(targetEvent, RDInput.holdingControl || RDInput.holdingShift);
				timeline.MoveToEvent(targetEvent, dontMoveIfMoving: addingOrRemovingEvent);
			}
		}

		[HarmonyPatch]
		public static class TimelineEventAddOrRemovePatch
		{
			[HarmonyPatch(typeof(List<LevelEvent>), nameof(List<LevelEvent>.Add))]
			[HarmonyPostfix]
			public static void OnEventAdded(List<LevelEvent> __instance, LevelEvent item)
			{
				scnEditor editor = scnEditor.instance;
				if (editor == null || editor.customLevel == null || editor.levelData == null || editor.isLoading || editor.events != __instance)
					return;

				ETLogger.Debug("Event Added");

				if (item != null && !TimelineConstants.TimelineIgnoreEvents.Contains(item.eventType))
				{
					timeline?.LevelEventAdded(item);
					addingOrRemovingEvent = true;
					scnEditor.instance.StartCoroutine(InvokeAtNextFrame(() => addingOrRemovingEvent = false));
				}
			}

			[HarmonyPatch(typeof(List<LevelEvent>), nameof(List<LevelEvent>.Remove))]
			[HarmonyPostfix]
			public static void OnEventRemoved(List<LevelEvent> __instance, LevelEvent item)
			{
				scnEditor editor = scnEditor.instance;
				if (editor == null || editor.customLevel == null || editor.levelData == null || editor.isLoading || editor.events != __instance)
					return;

				ETLogger.Debug("Event Removed");

				if (item != null && !TimelineConstants.TimelineIgnoreEvents.Contains(item.eventType))
				{
					timeline?.LevelEventRemoved(item);
					addingOrRemovingEvent = true;
					scnEditor.instance.StartCoroutine(InvokeAtNextFrame(() => addingOrRemovingEvent = false));
				}
			}

			[HarmonyPatch(typeof(List<LevelEvent>), nameof(List<LevelEvent>.RemoveAll))]
			[HarmonyPrefix]
			public static void OnEventRemovedRange(List<LevelEvent> __instance, Predicate<LevelEvent> match)
			{
				scnEditor editor = scnEditor.instance;
				if (editor == null || editor.customLevel == null || editor.levelData == null || editor.isLoading || editor.events != __instance)
					return;

				ETLogger.Debug("Event Removed All");

				List<LevelEvent> removeTarget = new List<LevelEvent>();
				foreach (var levelEvent in __instance)
				{
					if (levelEvent != null && !TimelineConstants.TimelineIgnoreEvents.Contains(levelEvent.eventType) && match(levelEvent))
						removeTarget.Add(levelEvent);
				}

				if (removeTarget.Count > 0)
				{
					timeline?.LevelEventRemovedRange(removeTarget);
					addingOrRemovingEvent = true;
					scnEditor.instance.StartCoroutine(InvokeAtNextFrame(() => addingOrRemovingEvent = false));
				}
			}
		}

		[HarmonyPatch]
		public static class TimelineLevelEventPatch
		{
			[HarmonyPatch]
			public static class EditorActionPatch
			{
				[HarmonyPatch(typeof(CopyFloorEditorAction), nameof(CopyFloorEditorAction.Execute))]
				[HarmonyPrefix]
				public static bool CopyMultipleEvents(scnEditor editor)
				{
					if (timeline.selectedEvents.Count > 0)
					{
						MultiCopyEvents(editor);
						return false;
					}
					return true;
				}

				[HarmonyPatch(typeof(CutFloorEditorAction), nameof(CutFloorEditorAction.Execute))]
				[HarmonyPrefix]
				public static bool CutMultipleEvents(scnEditor editor)
				{
					if (timeline.selectedEvents.Count > 0)
					{
						MultiCutEvents(editor);
						return false;
					}
					return true;
				}

				[HarmonyPatch(typeof(DeleteFloorsEditorAction), nameof(DeleteFloorsEditorAction.Execute))]
				[HarmonyPrefix]
				public static bool DeleteMultipleEvents(scnEditor editor)
				{
					if (timeline.selectedEvents.Count > 0)
					{
						DeleteMultiSelectionEvents(editor);
						return false;
					}
					return true;
				}
			}

			static bool cycleObjectSelection = false;

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.DeselectFloors))]
			[HarmonyPostfix]
			public static void ClearIfNeed()
			{
				if (timeline.clearSelectedEventsAtDeselectFloor && !cycleObjectSelection)
				{
					timeline.ClearSelect();
				}
			}

			[HarmonyPatch(typeof(scnEditor), "CycleObjectSelection")]
			[HarmonyPrefix]
			public static void PreCycleObject()
			{
				cycleObjectSelection = true;
			}

			[HarmonyPatch(typeof(scnEditor), "CycleObjectSelection")]
			[HarmonyPrefix]
			public static void PostCycleObject()
			{
				cycleObjectSelection = false;
			}

			[HarmonyPatch(typeof(LevelEvent), nameof(LevelEvent.IsDecoration), MethodType.Getter)]
			[HarmonyPostfix]
			public static void FakeResult(LevelEvent __instance, ref bool __result)
			{
				if (!__result && __instance.isFake)
					__result = true;
			}

			[HarmonyPatch(typeof(LevelEvent), nameof(LevelEvent.ApplyPropertiesToRealEvents))]
			[HarmonyPrefix]
			public static bool ApplyToRealEvents(LevelEvent __instance)
			{
				if (__instance.info.isDecoration) return true;
				if (!__instance.isFake) return true;

				foreach (string key in __instance.data.Keys)
				{
					if (__instance.disabled[key])
					{
						continue;
					}

					foreach (LevelEvent realEvent in __instance.realEvents)
					{
						global::ADOFAI.PropertyInfo propertyInfo = __instance.info.propertiesInfo[key];
						if (propertyInfo.name == "floor")
						{
							realEvent.floor = __instance.floor;
						}
						else if (propertyInfo.type == PropertyType.Vector2)
						{
							Vector2 vector = (Vector2)realEvent.data[key];
							Vector2 vector2 = (Vector2)__instance.data[key];
							if (!float.IsNaN(vector2.x))
							{
								vector.x = vector2.x;
							}

							if (!float.IsNaN(vector2.y))
							{
								vector.y = vector2.y;
							}

							realEvent.data[key] = vector;
						}
						else
						{
							realEvent.data[key] = __instance.data[key];
						}
					}
				}

				ADOBase.editor.ApplyEventsToFloors();
				timeline.Redraw();
				return false;
			}

			[HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.ShowPanel))]
			[HarmonyPrefix]
			public static bool ShowMultipleEvents(InspectorPanel __instance, LevelEventType eventType, int eventIndex)
			{
				if (eventType == LevelEventType.None) return true;
				if (!GCS.levelEventsInfo.ContainsKey(eventType.ToString())) return true;
				if (GCS.levelEventsInfo[eventType.ToString()].isDecoration) return true;
				if (undoOrRedoing || timeline.selectedEvents.Count == 0)
				{
					List<LevelEvent> selectedFloorEvents = ADOBase.editor.GetSelectedFloorEvents(eventType);
					if (eventIndex <= selectedFloorEvents.Count - 1)
					{
						timeline.ToggleSelectEvent(selectedFloorEvents[eventIndex], false, false);
					}
					return true;
				}
				if (timeline.selectedEvents.Count <= 1) return true;

				__instance.Set("showingPanel", true);
				__instance.cacheEventIndex = eventIndex;

				if (eventType != LevelEventType.None)
				{
					typeof(InspectorPanel).GetField("cacheSelectedEventType", AccessTools.all).SetValue(eventType, null);
				}
				using (new SaveStateScope(ADOBase.editor, false, false, false))
				{
					PropertiesPanel propertiesPanel = null;
					foreach (PropertiesPanel panel in __instance.panelsList)
					{
						if (panel.levelEventType == eventType)
						{
							panel.gameObject.SetActive(true);
							if (panel.tabContainer)
							{
								panel.tabContainer.gameObject.SetActive(true);
								PropertiesSubTabButton propertiesSubTabButton = panel.tabButtons.Values.First<PropertiesSubTabButton>();
								panel.SelectTab(propertiesSubTabButton.groupName);
							}
							__instance.titleCanvas.SetActive(true);
							propertiesPanel = panel;
						}
						else
						{
							panel.gameObject.SetActive(false);
							if (panel.tabContainer)
							{
								panel.tabContainer.gameObject.SetActive(false);
							}
						}
					}
					if (eventType != LevelEventType.None)
					{
						LevelEvent levelEvent = null;
						__instance.title.text = RDString.Get("editor." + eventType.ToString(), null, LangSection.Translations);
						LevelEvent fakeEvent = new LevelEvent(-1, timeline.selectedEvents.First().eventType)
						{
							isFake = true
						};
						bool isFirst = true;
						foreach (LevelEvent selectedEvent in timeline.selectedEvents)
						{
							if (selectedEvent.eventType != fakeEvent.eventType)
							{
								eventType = LevelEventType.None;
								__instance.titleCanvas.SetActive(value: false);
								__instance.HideAllInspectorTabs();
								__instance.Call("ModifyMessageText", new Type[] { typeof(string), typeof(float), typeof(bool) }, new object[] { RDString.Get("editortweaks.dialog.differentTypeEventSelected"), 0f, true });
								break;
							}

							fakeEvent.realEvents.Add(selectedEvent);
							bool hasEqualThings = false;
							foreach (string item in selectedEvent.data.Keys)
							{
								bool isFloor = item == "floor";
								object fakeEventData = (isFloor ? ((object)fakeEvent.floor) : fakeEvent.data[item]);
								object selectedEventData = (isFloor ? ((object)selectedEvent.floor) : selectedEvent.data[item]);
								global::ADOFAI.PropertyInfo propertyInfo = selectedEvent.info.propertiesInfo[item];
								bool eventEquals = __instance.Call<bool>("EventPropertyEquals", new object[] { propertyInfo, fakeEventData, selectedEventData });
								if (isFirst || (eventEquals && !fakeEvent.disabled[item]))
								{
									fakeEvent.disabled[item] = false;
									if (propertyInfo.type == PropertyType.Vector2)
									{
										Vector2 fakeVector = (Vector2)fakeEvent.data[item];
										Vector2 selectedVector = (Vector2)selectedEvent.data[item];
										bool vector2X = Mathf.Abs(fakeVector.x - selectedVector.x) < 0.0001f || isFirst;
										bool vector2Y = Mathf.Abs(fakeVector.y - selectedVector.y) < 0.0001f || isFirst;
										fakeEvent.data[item] = new Vector2(vector2X ? selectedVector.x : float.NaN, vector2Y ? selectedVector.y : float.NaN);
									}
									else if (isFloor)
									{
										fakeEvent.floor = selectedEvent.floor;
									}
									else
									{
										fakeEvent.data[item] = selectedEvent.data[item];
									}

									hasEqualThings = true;
								}
								else
								{
									fakeEvent.disabled[item] = true;
								}
							}

							if (!hasEqualThings)
							{
								break;
							}

							isFirst = false;
						}

						if (eventType != 0)
						{
							levelEvent = fakeEvent;
							__instance.Call("ModifyMessageText", new Type[] { typeof(string), typeof(bool) }, new object[] { "", false });
						}

						foreach (RectTransform tab in ADOBase.editor.levelEventsPanel.tabs)
						{
							InspectorTab component = tab.gameObject.GetComponent<InspectorTab>();
							if (!(component == null))
							{
								if (levelEvent != null && levelEvent.eventType == component.levelEventType)
								{
									component.gameObject.SetActive(value: true);
									component.GetComponent<RectTransform>().SetAnchorPosY(0f);
								}
								else
								{
									component.gameObject.SetActive(value: false);
								}
							}
						}

						if (propertiesPanel != null && levelEvent != null)
						{
							__instance.selectedEvent = levelEvent;
							__instance.selectedEventType = levelEvent.eventType;

							propertiesPanel.SetProperties(levelEvent);
							foreach (RectTransform tab2 in __instance.tabs)
							{
								InspectorTab component2 = tab2.gameObject.GetComponent<InspectorTab>();
								if (component2 == null)
								{
									continue;
								}

								if (eventType == component2.levelEventType)
								{
									component2.SetSelected(selected: true);
									component2.eventIndex = eventIndex;
								}
								else
								{
									component2.SetSelected(selected: false);
								}
							}
						}
					}
				}

				__instance.Set("showingPanel", false);
				return false;
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.PasteDecorations))]
			[HarmonyPrefix]
			public static bool PasteMultipleEvents(scnEditor __instance)
			{
				if (!__instance.clipboard.Any() || __instance.clipboardContent != scnEditor.ClipboardContent.Decorations || __instance.Get<bool>("dragging"))
					return true;

				if (!(__instance.clipboard[0] is LevelEvent) || ((LevelEvent)__instance.clipboard[0]).info.isDecoration)
					return true;

				using (new SaveStateScope(__instance))
				{
					timeline.ClearSelect();

					List<LevelEvent> copiedEvents = new List<LevelEvent>();
					foreach (LevelEvent clipboardItem in __instance.clipboard)
					{
						LevelEvent item = clipboardItem.Copy();
						copiedEvents.Add(item);
						__instance.events.Add(item);
						timeline.ToggleSelectEvent(item, true, false);
					}

					if (timeline.selectedEvents.Count == 1)
					{
						__instance.SelectFloor(__instance.floors[copiedEvents[0].floor]);
						__instance.levelEventsPanel.ShowPanelOfEvent(copiedEvents[0]);
					}
					else if (timeline.selectedEvents.Count > 1)
					{
						timeline.DeselectFloors(__instance, false, true);

						__instance.levelEventsPanel.ShowInspector(true, true);
						__instance.levelEventsPanel.ShowPanel(timeline.selectedEvents.First().eventType, 0);
					}

					copiedEvents.Clear();
				}
				return false;
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RemoveEventAtSelected))]
			[HarmonyPrefix]
			public static bool RemoveMultipleEvents(scnEditor __instance, LevelEventType eventType)
			{
				if (eventType == LevelEventType.None) return true;
				if (!__instance.SelectionIsEmpty()) return true;
				DeleteMultiSelectionEvents(__instance);
				return false;
			}

			[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.EnableEvent))]
			[HarmonyPostfix]
			public static void ActiveLevelEvent(LevelEvent e, bool enabled)
			{
				if (TimelineConstants.TimelineIgnoreEvents.Contains(e.eventType)) return;
				if (timeline.levelEventDatas[e].obj != null && timeline.levelEventDatas[e].obj.gameObject != null)
					timeline.levelEventDatas[e].obj.offImage.enabled = !enabled;
			}

			private static void MultiCopyEvents(scnEditor editor)
			{
				editor.clipboard.Clear();
				foreach (LevelEvent levelEvent in timeline.selectedEvents)
				{
					CopyEvent(editor, levelEvent, false, false);
				}
			}

			private static void MultiCutEvents(scnEditor editor)
			{
				using (new SaveStateScope(editor, false, true, false))
				{
					MultiCopyEvents(editor);
					DeleteMultiSelectionEvents(editor);
				}
			}

			private static void CopyEvent(scnEditor editor, LevelEvent toCopy, bool clearClipboard = true, bool cut = false)
			{
				if (clearClipboard)
				{
					editor.clipboard.Clear();
				}
				editor.clipboard.Add(toCopy.Copy());
				editor.clipboardContent = scnEditor.ClipboardContent.Decorations;
			}

			public static void DeleteMultiSelectionEvents(scnEditor editor)
			{
				if (timeline.selectedEvents.Count < 1)
				{
					return;
				}
				List<LevelEvent> list = new List<LevelEvent>(timeline.selectedEvents);
				editor.RemoveEvents(list);
			}
		}

		static IEnumerator InvokeAtNextFrame(Action action)
		{
			yield return null;
			action();
		}
	}
}
