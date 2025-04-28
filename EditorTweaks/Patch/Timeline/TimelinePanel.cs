using ADOFAI;
using DG.Tweening;
using EditorTweaks.Components;
using EditorTweaks.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace EditorTweaks.Patch.Timeline
{
	public class TimelinePanel : MonoBehaviour
	{
		struct DraggingEventData
		{
			public int floor;
			public float duration;
			public float angleOffset;
		}

		//public RectTransform root;
		public RectTransform content;
		public RectTransform grid;
		public RectTransform events;
		public ScrollRect scroll;
		public RectTransform selection;
		//public RectTransform floorNumBar;
		//public GameObject playhead;

		[Header("Prefabs")]
		public GameObject horizontalLine;
		public FloorNumberLine verticalLine;
		public GameObject eventObj;
		//public FloorNumberText floorNum;

		//private int vIndex;
		private readonly int[] magnetNums = new int[] { 1, 2, 3, 4, 6, 8, 12, 16, -1 };
		public int magnetNum { get; private set; } = 4;
		private int magnetNumIdx = 3;
		public bool followPlayhead = true;
		//private bool changingScroll = false;
		private Vector2 prevScrollPos = Vector2.zero;

		private int firstLineShowingOnScreenIdx = -1;
		private int lastLineShowingOnScreenIdx = -1;

		//private TimelineEvent selectedEvent;

		private ObjectPool<GameObject> hPool;
		private ObjectPool<FloorNumberLine> vPool;
		private ObjectPool<GameObject> eventPool;
		//private ObjectPool<FloorNumberText> floorNumPool;

		private LinkedList<VerticalLineData> vLines = new LinkedList<VerticalLineData>();
		public LevelEventDataCollection levelEventDatas = new LevelEventDataCollection();

		private Dictionary<LevelEvent, GameObject> showingEvents = new Dictionary<LevelEvent, GameObject>();

		private Tween timelineAutoMoveTween;

		private bool isDragging = false;
		private bool isDraggingEvents = false;
		private Vector2 selectionStartPos = Vector2.zero;
		private bool showingSelection = false;
		private LinkedListNode<LevelEventData> selectionStartNode = null;
		private LinkedListNode<LevelEventData> selectionEndNode = null;

		public HashSet<LevelEvent> selectedEvents = new HashSet<LevelEvent>();
		private Dictionary<LevelEvent, DraggingEventData> draggingEventDatas = new Dictionary<LevelEvent, DraggingEventData>();

		public bool clearSelectedEventsAtDeselectFloor = true;

		private bool redrawPending = false;

		private float timelineHeight = 0f;

		//private int selectingEventFloor = -1;
		//private LevelEventData selectingTargetEvent;

		void CreatePool()
		{
			hPool = new ObjectPool<GameObject>(
				() => Instantiate(horizontalLine, grid),
				(obj) => obj.SetActive(true),
				(obj) => obj.SetActive(false),
				(obj) => Destroy(obj),
				true,
				20,
				100
			);
			vPool = new ObjectPool<FloorNumberLine>(
				() => Instantiate(verticalLine, grid),
				(obj) => obj.gameObject.SetActive(true),
				(obj) => obj.gameObject.SetActive(false),
				(obj) => Destroy(obj),
				true,
				20,
				1000
			);
			eventPool = new ObjectPool<GameObject>(
				() => Instantiate(eventObj, events),
				(obj) => obj.SetActive(true),
				(obj) => obj.SetActive(false),
				(obj) => Destroy(obj),
				true,
				20,
				10000
			);

			scroll.onValueChanged.AddListener(OnValueChanged);
		}

		public void Init(bool optimizeRows = true, float posX = 0f, float posY = 1f)
		{
			scnEditor editor = scnEditor.instance;
			if (vPool == null)
			{
				//ETLogger.Debug("Creating pool.");
				CreatePool();
			}

			// release all loaded objects

			//Main.Entry.Logger.Log("[d] TimelinePanel#Init() called. Releasing objects to pool...");

			//ETLogger.Debug("Releasing vLines.");
			foreach (var line in vLines)
			{
				// verticalLine obj and floorNum obj can be null
				// when floor is midspin
				if (line.obj != null)
				{
					vPool.Release(line.obj);
				}
			}
			vLines.Clear();

			//ETLogger.Debug("Releasing levelEvents.");
			foreach (var events in levelEventDatas)
			{
				var eventData = events.Value;
				if (eventData.obj != null && eventData.obj.gameObject != null)
				{
					//eventData.obj.GetComponent<TimelineEvent>().isRendering = false;
					ReleaseEventObject(eventData.obj, events.Key);

					eventData.obj.targetEvent = null;
				}
			}
			levelEventDatas.Clear();

			//Main.Entry.Logger.Log(string.Format("[d] eventPool CountAll: {0} active: {1} inactive: {2}", eventPool.CountAll, eventPool.CountActive, eventPool.CountInactive));

			//ETLogger.Debug("Reload Start.");
			Reload(optimizeRows);
			//ETLogger.Debug("Reload End.");

			//floorNumBar.GetComponent<RectTransform>().SizeDeltaX(timelineWidth);

			Vector2 pos = new Vector2(posX, posY);
			scroll.normalizedPosition = pos;
			prevScrollPos = pos;
			// move scroll position after initialization completed ( Init() then OnValueChanged() )
			//scroll.content.anchoredPosition = Vector2.zero;

			selection.position = Vector2.zero;

			//ETLogger.Debug("Redraw Start");
			Redraw();
			//ETLogger.Debug("Redraw End");
		}

		private void Reload(bool optimizeRows)
		{
			scnEditor editor = scnEditor.instance;

			var floors = editor.floors;

			float scrollWidth = scroll.viewport.rect.width;
			float scrollHeight = scroll.viewport.rect.height;

			// release temporary levelevent object
			//if (selectingTargetEvent != null)
			//{
			//    eventPool.Release(selectingTargetEvent.obj);
			//    selectingTargetEvent.obj = null;
			//}

			//selectingTargetEvent = null;
			//selectingEventFloor = -1;

			// loop level floors(tiles) and register verticalLine

			//ETLogger.Debug("Register vLines.");

			firstLineShowingOnScreenIdx = -1;
			lastLineShowingOnScreenIdx = -1;

			for (int i = 0; i < floors.Count; i++)
			{
				var floor = floors[i];

				if (floor.midSpin)
					continue;

				float posX = GetLinePosX(floor);
				if (posX < scrollWidth)
				{
					var lineData = CreateLineData(floor, i);
					vLines.AddLast(lineData);

					lastLineShowingOnScreenIdx++;
				}
				if (lastLineShowingOnScreenIdx >= 0)
					firstLineShowingOnScreenIdx = 0;
			}

			// last LevelEvent's end time on each timeline row
			// used to calculate the optimal row for next LevelEvent
			List<float> timelineRowEndPosX = new List<float>();
			int timelineRowCount = 0;

			// loop level floors(tiles) and register level events

			//ETLogger.Debug("Register levelEvents, " + (optimizeRows ? "optimize rows.": "dont optimize rows."));

			foreach (var levelEvent in editor.events)
			{
				if (TimelineConstants.TimelineIgnoreEvents.Contains(levelEvent.eventType))
					continue;
				//if (levelEventDatas.ContainsKey(levelEvent))
				//	continue;

				// get position
				Vector2 position = new Vector2(ETUtils.GetEventPosX(levelEvent), -25);

				float objWidth = ETUtils.GetEventObjWidth(levelEvent);

				// get the optimal timeline row
				int optimalRow = -1;

				if (optimizeRows)
				{
					float eventEndPosX = position.x + objWidth;
					for (int i = 0; i < timelineRowEndPosX.Count; i++)
					{
						// Main.Entry.Logger.Log(string.Format("i: {0} endPosX: {1} curPosX: {2}", i, timelineRowEndPosX[i], position.x));
						if (timelineRowEndPosX[i] < position.x)
						{
							optimalRow = i;
							timelineRowEndPosX[i] = eventEndPosX;
							break;
						}
					}
					if (optimalRow < 0)
					{
						// no optimal row found, create a new row and place it
						optimalRow = timelineRowEndPosX.Count;
						timelineRowEndPosX.Add(eventEndPosX);
					}
					timelineRowCount = timelineRowEndPosX.Count;
					ETUtils.SetRowNumber(levelEvent, optimalRow);
				}
				else
				{
					if (levelEvent.data.ContainsKey("row"))
					{
						optimalRow = ETUtils.GetRowNumber(levelEvent);
						if (optimalRow < 0)
							optimalRow = 0;
						timelineRowCount = Math.Max(timelineRowCount, optimalRow + 1);
					}
					else
					{
						ETUtils.SetRowNumber(levelEvent, 0);
						optimalRow = 0;
					}
				}

				//Main.Entry.Logger.Log(string.Format("floor {0}: {1} {2} {3} // row: {4}", levelEvent.floor, levelEvent.eventType, position.x, eventEndPosX, optimalRow));

				var eventData = new LevelEventData(optimalRow, levelEvent);
				levelEventDatas.Add(levelEvent, eventData, false);

				//if (position.x <= scrollWidth)
				//{
				//	//Main.Entry.Logger.Log("[d] (init phase) adding event | floor " + levelEvent.floor + " type: " + levelEvent.eventType);

				//	var obj = CreateEventObject(levelEvent, position.x, eventData.timelineRow, objWidth);

				//	if (obj != null)
				//	{
				//		eventData.obj = obj;
				//	}
				//}
			}

			//ETLogger.Debug("Register ended.");
			//ETLogger.Debug("Sorting events.");
			levelEventDatas.Sort();
			//ETLogger.Debug("Sort ended.");

			//Main.Entry.Logger.Log("[d] [init] events: startPosSortedIdx = " + levelEventsSortedByStartPosListEndIdx + ", endPosSortedIdx = " + levelEventsSortedByEndPosListStartIdx);

			Resize();
			timelineHeight = Mathf.Max((timelineRowCount + 2) * ETUtils.timelineHeight, 300f);
			content.SizeDeltaY(Mathf.Max((timelineRowCount + 2) * ETUtils.timelineHeight, 300f));

			//ETLogger.Debug("Reload ended.");
		}

		public void SetParent(RectTransform transform)
		{
			this.transform.SetParent(transform, false);
		}

		public void MoveToFloor(scrFloor floor, bool dontMoveIfMoving = false)
		{
			if (!Main.ETConfig.timelineJumpToFloor) return;
			if (!scrController.instance.paused) return;
			if (floor == null) return;
			if (dontMoveIfMoving && timelineAutoMoveTween != null) return;
			float x = ETUtils.TimeToBeat(floor.entryTime) * ETUtils.timelineWidth / (content.sizeDelta.x - scroll.viewport.rect.width) - scroll.viewport.rect.width / content.sizeDelta.x / 4f;
			x = Mathf.Clamp01(x);
			Vector2 endValue = new Vector2(x, scroll.normalizedPosition.y);
			timelineAutoMoveTween?.Kill();
			timelineAutoMoveTween = DOTween.To(() => scroll.normalizedPosition, (v) => scroll.normalizedPosition = v, endValue, 0.3f).SetEase(Ease.OutCubic).SetUpdate(true).OnUpdate(() => OnValueChanged(scroll.normalizedPosition)).OnKill(() => timelineAutoMoveTween = null);
		}

		public void MoveToEvent(LevelEvent levelEvent, bool immediately = false, bool dontMoveIfMoving = false)
		{
			if (!Main.ETConfig.timelineJumpToEvent) return;
			if (!scrController.instance.paused) return;
			if (levelEvent == null) return;
			if (dontMoveIfMoving && timelineAutoMoveTween != null) return;
			if (!levelEventDatas.ContainsKey(levelEvent)) return;
			float x = ETUtils.GetEventPosX(levelEvent) / (content.sizeDelta.x - scroll.viewport.rect.width) - scroll.viewport.rect.width / content.sizeDelta.x / 4f;
			x = Mathf.Clamp01(x);
			float y = levelEventDatas[levelEvent].timelineRowNumber * ETUtils.timelineHeight / content.sizeDelta.y;
			y = 1f - Mathf.Clamp01(y);
			Vector2 endValue = new Vector2(x, y);
			timelineAutoMoveTween?.Kill();
			if (immediately)
			{
				scroll.normalizedPosition = endValue;
				OnValueChanged(scroll.normalizedPosition);
			}
			else
			{
				timelineAutoMoveTween = DOTween.To(() => scroll.normalizedPosition, (v) => scroll.normalizedPosition = v, endValue, 0.3f).SetEase(Ease.OutCubic).SetUpdate(true).OnUpdate(() => OnValueChanged(scroll.normalizedPosition)).OnKill(() => timelineAutoMoveTween = null);
			}
		}

		void Update()
		{
			if (!scrController.instance.paused)
			{
				scrConductor conductor = scrConductor.instance;
				if (conductor.song.clip)
				{
					//playhead.transform.LocalMoveX(TimeToBeat(conductor.songposition_minusi) * width);

					if (followPlayhead)
					{
						float x = ETUtils.TimeToBeat(conductor.songposition_minusi) * ETUtils.timelineWidth;
						x = x / (content.sizeDelta.x - scroll.viewport.rect.width) - scroll.viewport.rect.width / content.sizeDelta.x / 4f;
						x = Mathf.Clamp01(x);
						Vector2 position = new Vector2(x, scroll.normalizedPosition.y);
						scroll.normalizedPosition = position;
						OnValueChanged(position);
						//changingScroll = true;
						//scroll.content.anchoredPosition = new Vector2(
						//	-playhead.transform.localPosition.x + root.rect.width / 2,
						//	scroll.content.anchoredPosition.y
						//);
						//changingScroll = false;
					}
				}
			}

			OnDrag();
		}

		//void OnGUI()
		//{
		//	//string s = "";
		//	//s += selectedEvents.Count + "\n";
		//	//foreach (var levelEvent in selectedEvents)
		//	//{
		//	//	s += levelEvent.eventType + "\n";
		//	//}
		//	//GUIStyle gui = new GUIStyle();
		//	//gui.fontSize = 30;
		//	//gui.font = RDString.GetFontDataForLanguage(RDString.language).font;
		//	//gui.normal.textColor = Color.white;
		//	//GUI.Label(new Rect(2, 2, Screen.width, Screen.height), s, gui);

		//	string GetPosition(bool startPos)
		//	{
		//		var viewport = levelEventDatas.GetNode(startPos);
		//		int idx = 0;
		//		if (levelEventDatas.IsOutOfRange(startPos))
		//		{
		//			idx = levelEventDatas.IsMinus(startPos) ? -1 : /*levelEventDatas.SortedByEndPos.Count*/int.MaxValue;
		//		}
		//		else
		//		{
		//			int i = 0;
		//			foreach (var data in startPos ? levelEventDatas.sortedByEndPos : levelEventDatas.sortedByStartPos)
		//			{
		//				if (data == viewport.Value)
		//				{
		//					idx = i;
		//					break;
		//				}
		//				i++;
		//			}
		//		}
		//		return idx.ToString();
		//	}

		//	string s1 = "";
		//	string s2 = "";
		//	var node = levelEventDatas.sortedByStartPos.First;
		//	for (int i = 0; i < 20; i++)
		//	{
		//		if (node == null) break;
		//		s1 += ETUtils.GetEventPosX(node.Value.evt) + "\n";
		//		node = node.Next;
		//	}

		//	node = levelEventDatas.sortedByEndPos.First;
		//	for (int i = 0; i < 20; i++)
		//	{
		//		if (node == null) break;
		//		s2 += ETUtils.GetEventPosX(node.Value.evt) + ETUtils.GetEventObjWidth(node.Value.evt) + "\n";
		//		node = node.Next;
		//	}

		//	int poolSize = eventPool.CountAll;
		//	int poolActiveSize = eventPool.CountActive;
		//	int poolInactiveSize = eventPool.CountInactive;

		//	GUIStyle gui = new GUIStyle();
		//	gui.fontSize = 30;
		//	gui.font = RDString.GetFontDataForLanguage(RDString.language).font;
		//	gui.normal.textColor = Color.white;
		//	GUI.Label(new Rect(2, 2, 250, Screen.height), s1, gui);
		//	GUI.Label(new Rect(252, 2, 250, Screen.height), s2, gui);
		//	GUI.Label(new Rect(502, 2, Screen.width, Screen.height), $"Viewport Start Idx: {GetPosition(true)}\nViewport End Idx: {GetPosition(false)}\nPool Size: {poolSize}\nPool Active: {poolActiveSize}\nPool Inactive: {poolInactiveSize}", gui);
		//}

		public void OnValueChanged(Vector2 position)
		{
			scnEditor editor = scnEditor.instance;

			Vector2 pos = position * (content.sizeDelta - scroll.viewport.rect.size);
			Vector2 dir = prevScrollPos - pos;

			// scrolled to the right
			if (dir.x < 0)
			{
				int frontLinesToRemove = 0;
				foreach (var line in vLines)
				{
					if (line.x < pos.x)
					{
						// È­¸é ¿ÞÂÊÀ¸·Î ³ª°£ lineµéÀ» ¾ð·Îµå
						if (line.obj != null)
							vPool.Release(line.obj);

						frontLinesToRemove++;
					}
					else
						break;
				}

				for (int i = 0; i < frontLinesToRemove; i++)
				{
					// foreach ¾È¿¡¼­ list itemÀ» ¾Õ¿¡¼­ºÎÅÍ »èÁ¦ÇÏ´Â °ÍÀÌ ºÒ°¡´ÉÇÏ¹Ç·Î
					// »èÁ¦ÇÒ line °³¼ö¸¦ ¼¾ ´ÙÀ½ µû·Î ·çÇÁµ¹·Á¼­ »èÁ¦
					vLines.RemoveFirst();
				}

				firstLineShowingOnScreenIdx += frontLinesToRemove;

				lastLineShowingOnScreenIdx = firstLineShowingOnScreenIdx + vLines.Count - 1;
				for (int i = lastLineShowingOnScreenIdx + 1; i < editor.floors.Count; i++)
				{
					if (i < 0)
						i = 0;

					var floor = editor.floors[i];
					float posX = GetLinePosX(floor);
					if (posX < pos.x)
					{
						// ³Ê¹« ±ä ±æÀÌ¸¦ °Ç³Ê¶Ù¾î¼­ vLines list¿¡ ¾Æ¹«°Íµµ ¾øÀ» °æ¿ì (pos.x º¯°æ Àü°ú ÈÄ ÁöÁ¡¿¡¼­ °ãÄ¡´Â lineÀÌ ¾øÀ» °æ¿ì)
						// ·çÇÁ¸¦ ¼øÂ÷ÀûÀ¸·Î µ¹¸é¼­ Ã³À½À¸·Î Ç¥½ÃµÇ¾î¾ß ÇÒ lineÀ» Ã£±â
						firstLineShowingOnScreenIdx = i + 1;
						lastLineShowingOnScreenIdx = firstLineShowingOnScreenIdx;
						continue;
					}
					if (posX > pos.x + scroll.viewport.rect.width)
						break;

					vLines.AddLast(CreateLineData(floor, i));

					lastLineShowingOnScreenIdx++;
				}
			}
			// scrolled to the left
			else if (dir.x > 0)
			{
				int backLinesToRemove = 0;
				foreach (var line in vLines.Reverse())
				{
					if (line.x > pos.x + scroll.viewport.rect.width)
					{
						if (line.obj != null)
							vPool.Release(line.obj);

						backLinesToRemove++;
					}
				}

				for (int i = 0; i < backLinesToRemove; i++)
				{
					vLines.RemoveLast();
				}

				lastLineShowingOnScreenIdx -= backLinesToRemove;

				firstLineShowingOnScreenIdx = lastLineShowingOnScreenIdx - vLines.Count + 1;
				for (int i = firstLineShowingOnScreenIdx - 1; i >= 0; i--)
				{
					if (i >= editor.floors.Count)
						i = editor.floors.Count - 1;

					var floor = editor.floors[i];
					float posX = GetLinePosX(floor);
					if (posX > pos.x + scroll.viewport.rect.width)
					{
						lastLineShowingOnScreenIdx = i - 1;
						firstLineShowingOnScreenIdx = lastLineShowingOnScreenIdx;
						continue;
					}
					if (posX < pos.x)
						break;

					vLines.AddFirst(CreateLineData(floor, i));

					firstLineShowingOnScreenIdx--;
				}
			}

			if (dir.x != 0)
			{
				bool movingLeft = dir.x < 0;

				// remove level events object which is completely hidden to the side of viewport
				var data = levelEventDatas.GetNode(movingLeft);

				if (levelEventDatas.IsOutOfRange(movingLeft))
				{
					data = levelEventDatas.GetLast(movingLeft);
				}

				while (data != null)
				{
					float startPosX = ETUtils.GetEventPosX(data.Value.evt);
					float endPosX = startPosX + ETUtils.GetEventObjWidth(data.Value.evt);

					if (movingLeft)
					{
						//if (startPosX > prevScrollPos.x + scroll.viewport.rect.width)
						//{
						//	levelEventDatas.Move(movingLeft, false);
						//	data = levelEventDatas.GetNode(movingLeft);
						//	continue;
						//}

						if (endPosX >= pos.x)
							break;
					}
					else
					{
						//if (endPosX < prevScrollPos.x)
						//{
						//	levelEventDatas.Move(!movingLeft, false);
						//	data = levelEventDatas.GetNode(!movingLeft);
						//	continue;
						//}

						if (startPosX <= pos.x + scroll.viewport.rect.width)
							break;
					}

					if (data.Value.obj != null && data.Value.obj.gameObject != null)
					{
						ReleaseEventObject(data.Value.obj, data.Value.evt);
						data.Value.obj.targetEvent = null;
					}

					levelEventDatas.Move(movingLeft, false);
					data = levelEventDatas.GetNode(movingLeft);
				}

				// add level events object which is shown to the side of viewport
				data = levelEventDatas.GetNode(!movingLeft);

				if (levelEventDatas.IsOutOfRange(!movingLeft))
				{
					data = levelEventDatas.GetFirst(!movingLeft);
				}

				while (data != null)
				{
					var startPosX = ETUtils.GetEventPosX(data.Value.evt);
					var endPosX = startPosX + ETUtils.GetEventObjWidth(data.Value.evt);

					if (movingLeft)
					{
						if (startPosX > pos.x + scroll.viewport.rect.width)
							break;
						if (endPosX < pos.x)
						{
							levelEventDatas.Move(!movingLeft, true);
							data = levelEventDatas.GetNode(!movingLeft);
							continue;
						}
					}
					else
					{
						if (endPosX < pos.x)
							break;
						if (startPosX > pos.x + scroll.viewport.rect.width)
						{
							levelEventDatas.Move(!movingLeft, true);
							data = levelEventDatas.GetNode(!movingLeft);
							continue;
						}
					}

					var obj = CreateEventObject(data.Value.evt, startPosX, data.Value.timelineRowNumber, endPosX - startPosX);

					if (obj != null)
						data.Value.obj = obj;
					levelEventDatas.Move(!movingLeft, true);
					data = levelEventDatas.GetNode(!movingLeft);
				}
			}

			//if (!changingScroll && dir.x != 0)
			//    followPlayhead = false;

			prevScrollPos = pos;
		}

		public void LevelEventAdded(LevelEvent levelEvent)
		{
			int optimalRow = -1;

			if (levelEvent.data.ContainsKey("row"))
			{
				optimalRow = ETUtils.GetRowNumber(levelEvent);
				if (optimalRow < 0)
				{
					ETUtils.SetRowNumber(levelEvent, 0);
					optimalRow = 0;
				}
			}
			else
			{
				optimalRow = 0;
			}

			//Main.Entry.Logger.Log(string.Format("floor {0}: {1} {2} {3} // row: {4}", levelEvent.floor, levelEvent.eventType, position.x, eventEndPosX, optimalRow));

			var eventData = new LevelEventData(optimalRow, levelEvent);
			levelEventDatas.Add(levelEvent, eventData);

			if (scnEditor.instance.isLoading) return;

			Resize();

			StartRedraw();
			MoveToEvent(levelEvent, true);
		}

		public void LevelEventRemoved(LevelEvent levelEvent)
		{
			if (levelEventDatas[levelEvent].obj != null && levelEventDatas[levelEvent].obj.gameObject != null)
			{
				ReleaseEventObject(levelEventDatas[levelEvent].obj, levelEvent);
				levelEventDatas[levelEvent].obj.targetEvent = null;
			}

			levelEventDatas.Remove(levelEvent);

			if (scnEditor.instance.isLoading) return;

			Resize();

			StartRedraw();
		}

		public void LevelEventRemovedRange(List<LevelEvent> levelEvents)
		{
			foreach (var levelEvent in levelEvents)
			{
				if (levelEventDatas[levelEvent].obj != null && levelEventDatas[levelEvent].obj.gameObject != null)
				{
					ReleaseEventObject(levelEventDatas[levelEvent].obj, levelEvent);
					levelEventDatas[levelEvent].obj.targetEvent = null;
				}

				levelEventDatas.Remove(levelEvent);
			}

			if (scnEditor.instance.isLoading) return;

			Resize();

			StartRedraw();
		}

		public void StartRedraw()
		{
			if (!redrawPending)
				StartCoroutine(RedrawAtEndOfFrame());
		}

		public IEnumerator RedrawAtEndOfFrame()
		{
			if (redrawPending) yield break;
			redrawPending = true;
			yield return new WaitForEndOfFrame();
			Redraw();
			redrawPending = false;
			yield break;
		}

		public void Redraw()
		{
			Vector2 pos = scroll.normalizedPosition * (content.sizeDelta - scroll.viewport.rect.size);
			prevScrollPos = pos;
			float posStart = pos.x;
			float posEnd = posStart + scroll.viewport.rect.width;

			Rect viewport = new Rect(posStart, 0f, scroll.viewport.rect.width, float.MaxValue);

			foreach (var events in levelEventDatas)
			{
				var eventData = events.Value;
				if (eventData.obj != null && eventData.obj.gameObject != null)
				{
					ReleaseEventObject(eventData.obj, events.Key);

					eventData.obj.targetEvent = null;
				}
			}

			CreateEvents(true);
			CreateEvents(false);

			void CreateEvents(bool startPos)
			{
				LinkedListNode<LevelEventData> node = levelEventDatas.GetLast(startPos);
				LinkedListNode<LevelEventData> nearestNode = null;

				while (node != null)
				{
					float eventStart = ETUtils.GetEventPosX(node.Value.evt);
					float eventEnd = eventStart + ETUtils.GetEventObjWidth(node.Value.evt);
					Rect eventRect = new Rect(eventStart, 0f, ETUtils.GetEventObjWidth(node.Value.evt), float.MaxValue);

					if (viewport.Overlaps(eventRect))
					{
						if (nearestNode == null) nearestNode = node;
						var obj = CreateEventObject(node.Value.evt, eventStart, node.Value.timelineRowNumber, eventEnd - eventStart);
						if (obj != null)
							node.Value.obj = obj;
					}

					node = startPos ? node.Next : node.Previous;
				}

				levelEventDatas.SetIdx(startPos, nearestNode);
			}
		}

		public void Resize()
		{
			scrConductor conductor = scrConductor.instance;
			float timelineWidth = ETUtils.TimeToBeat(scnEditor.instance.floors.Last().entryTime + conductor.crotchetAtStart * 2) * ETUtils.timelineWidth;

			if (levelEventDatas.sortedByEndPos.Count > 0)
			{
				LevelEvent lastEvent = levelEventDatas.sortedByEndPos.Last.Value.evt;
				float lastEventBasedWidth = ETUtils.GetEventPosX(lastEvent) + ETUtils.GetEventObjWidth(lastEvent);
				timelineWidth = Mathf.Max(timelineWidth, lastEventBasedWidth);
			}

			content.SizeDeltaX(timelineWidth);
			content.SizeDeltaY(Mathf.Max(((RectTransform)transform).sizeDelta.y, timelineHeight));
			//content.SizeDeltaY(Mathf.Max((timelineRowCount + 2) * ETUtils.timelineHeight, 300f));
		}

		private VerticalLineData CreateLineData(scrFloor floor, int floorIdx)
		{
			scnEditor editor = scnEditor.instance;

			if (floor.midSpin)
			{
				// find first previous floor which is not a midspin
				// NOTE: can throw exception when no non-midspin floor exist before floorIdx
				// But that situation should not occur since valid chart data always starts with non-midspin floor
				scrFloor prevNormalFloor = null;
				for (int i = floorIdx - 1; i >= 0; i--)
				{
					if (!editor.floors[i].midSpin)
					{
						prevNormalFloor = editor.floors[i];
						break;
					}
				}
				var prevLineX = GetLinePosX(prevNormalFloor);

				return new VerticalLineData(floor.seqID, prevLineX, null);
			}

			var line = vPool.Get();
			float posX = GetLinePosX(floor);
			line.transform.LocalMoveX(posX);
			line.text.text = floor.seqID.ToString();

			line.gameObject.SetActive(true);
			return new VerticalLineData(floor.seqID, posX, line);
		}

		private TimelineEvent CreateEventObject(LevelEvent levelEvent, float posX, int timelineRow, float objWidth)
		{
			if (showingEvents.ContainsKey(levelEvent))
			{
				//ETLogger.Warn("Already showing event!");
				return null;
			}
			if (levelEvent == null || levelEvent.isFake)
			{
				ETLogger.Warn("LevelEvent is null or fake!");
				return null;
			}
			var obj = eventPool.Get();
			if (levelEvent == null)
			{
				obj.transform.GetChild(2).gameObject.SetActive(false);
				obj.GetComponent<Image>().color = TimelineConstants.DefaultEventColor.WithAlpha(0.75f);
			}
			else
			{
				showingEvents.Add(levelEvent, obj);
				obj.transform.GetChild(2).GetComponent<Image>().sprite = GCS.levelEventIcons[levelEvent.eventType];
				obj.GetComponent<Image>().color = TimelineConstants.EventColors[levelEvent.eventType].WithAlpha(0.75f);
			}
			var timelineEvent = obj.GetComponent<TimelineEvent>();
			timelineEvent.panel = this;
			timelineEvent.targetEvent = levelEvent;
			//timelineEvent.isRendering = true;
			//timelineEvent.button.interactable = levelEvent == null ? false : (selectedEvent?.targetEvent) != levelEvent;
			timelineEvent.overlayImage.enabled = selectedEvents.Contains(levelEvent);
			timelineEvent.offImage.enabled = !levelEvent.active;

			obj.transform.LocalMoveX(posX);
			obj.transform.LocalMoveY(-timelineRow * ETUtils.timelineHeight);
			obj.GetComponent<RectTransform>().SizeDeltaX(objWidth);

			//Main.Entry.Logger.Log(string.Format("[d] CreateEventObject: floor {0} {1}, posX: {2}", levelEvent.floor, levelEvent.eventType, posX));
			return timelineEvent;
		}

		private void ReleaseEventObject(TimelineEvent eventObject, LevelEvent levelEvent)
		{
			eventObject.overlayImage.enabled = false;
			eventObject.offImage.enabled = false;
			showingEvents.Remove(levelEvent);
			eventPool.Release(eventObject.gameObject);
		}

		private float GetLinePosX(scrFloor floor)
		{
			return ETUtils.TimeToBeat(floor.entryTime) * ETUtils.timelineWidth * ETUtils.timelineScale + ETUtils.timelineHeight;
		}

		public void ClearSelect()
		{
			foreach (var levelEvent in showingEvents.Keys)
			{
				if (levelEventDatas[levelEvent].obj != null)
					levelEventDatas[levelEvent].obj.overlayImage.enabled = false;
			}
			selectedEvents.Clear();
			scnEditor.instance.levelEventsPanel.HideAllInspectorTabs();
		}

		public IEnumerator SelectEventAtEndOfFrame(LevelEvent levelEvent, bool shouldSelect)
		{
			yield return new WaitForEndOfFrame();
			if (TimelineConstants.TimelineIgnoreEvents.Contains(levelEvent.eventType)) yield break;
			if (levelEventDatas[levelEvent].obj != null && levelEventDatas[levelEvent].obj.gameObject != null)
			{
				levelEventDatas[levelEvent].obj.overlayImage.enabled = shouldSelect;
			}
			yield break;
		}

		public void ToggleSelectEvent(LevelEvent levelEvent, bool additive = false, bool showPanel = true)
		{
			if (!scrController.instance.paused) return;
			if (levelEvent == null) return;
			bool shouldSelect = true;
			if (selectedEvents.Contains(levelEvent)) shouldSelect = false;
			if (!additive) ClearSelect();
			if (shouldSelect)
				selectedEvents.Add(levelEvent);
			else
				selectedEvents.Remove(levelEvent);
			if (!levelEventDatas.ContainsKey(levelEvent))
			{
				StartCoroutine(SelectEventAtEndOfFrame(levelEvent, shouldSelect));
			}
			else if (levelEventDatas[levelEvent].obj != null && levelEventDatas[levelEvent].obj.gameObject != null)
			{
				levelEventDatas[levelEvent].obj.overlayImage.enabled = shouldSelect;
			}

			if (!showPanel) return;

			scnEditor editor = scnEditor.instance;
			if (selectedEvents.Count == 1)
			{
				editor.SelectFloor(editor.floors[levelEvent.floor]);
				editor.levelEventsPanel.ShowPanelOfEvent(levelEvent);
			}
			else if (selectedEvents.Count > 1)
			{
				DeselectFloors(editor, false, true);

				editor.levelEventsPanel.ShowInspector(true, true);
				editor.levelEventsPanel.ShowPanel(selectedEvents.First().eventType, 0);
			}
			else
			{
				DeselectFloors(editor, false, true);
			}
		}

		public void ToggleActiveEvent(LevelEvent levelEvent)
		{
			if (selectedEvents.Contains(levelEvent))
			{
				foreach (LevelEvent evt in selectedEvents)
				{
					scnEditor.instance.EnableEvent(evt, !evt.active);
				}
			}
			else
				scnEditor.instance.EnableEvent(levelEvent, !levelEvent.active);
		}

		public int SetMagnet()
		{
			magnetNumIdx++;
			if (magnetNumIdx >= magnetNums.Length)
				magnetNumIdx = 0;
			if (magnetNums[magnetNumIdx] < 0)
			{
				magnetNum = 64;
				return -1;
			}
			magnetNum = magnetNums[magnetNumIdx];
			return magnetNum;
		}

		public void SetMagnet(int magnet)
		{
			if (magnet < 1) magnet = 64;
			magnetNum = magnet;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!scrController.instance.paused) return;
			isDragging = true;
			if (selectedEvents.Count > 0)
			{
				foreach (var levelEvent in selectedEvents)
				{
					if (eventData.selectedObject != null && eventData.selectedObject.transform.GetComponent<TimelineEvent>() != null)
					{
						if (eventData.selectedObject.GetComponent<TimelineEvent>().targetEvent == levelEvent)
						{
							scnEditor editor = scnEditor.instance;

							isDraggingEvents = true;

							draggingEventDatas.Clear();
							foreach (var evt in selectedEvents)
							{
								draggingEventDatas.Add(evt, new DraggingEventData()
								{
									floor = evt.floor,
									duration = evt.data.ContainsKey("duration") ? (float)evt["duration"] / editor.floors[evt.floor].speed : 0,
									angleOffset = evt.data.ContainsKey("angleOffset") ? (float)evt["angleOffset"] / editor.floors[evt.floor].speed : 0
								});
							}

							Vector2 contentPosition;
							RectTransformUtility.ScreenPointToLocalPointInRectangle(content, Input.mousePosition, null, out contentPosition);

							contentPosition.x += content.rect.width;
							contentPosition.y = -contentPosition.y - 25f;

							int startFloor = Mathf.Clamp(firstLineShowingOnScreenIdx, 0, editor.floors.Count - 1);
							//ETLogger.Debug("startFloor: " + startFloor);
							scrFloor floor = editor.floors[startFloor];
							float offsetX = GetLinePosX(floor);

							while (floor.nextfloor != null)
							{
								if (GetLinePosX(floor) > contentPosition.x)
									break;
								offsetX = GetLinePosX(floor);
								floor = floor.nextfloor;
							}

							for (int i = 1; i < magnetNum; i++)
							{
								if (offsetX + (ETUtils.timelineWidth / magnetNum) > contentPosition.x)
									break;
								offsetX += ETUtils.timelineWidth / magnetNum;
							}

							float offsetY = contentPosition.y % ETUtils.timelineHeight;

							using (new SaveStateScope(scnEditor.instance))
							{

							}

							return;
						}
					}
				}
			}
			showingSelection = true;
			selectionStartNode = levelEventDatas.GetNode(true);
			if (levelEventDatas.IsOutOfRange(true))
				selectionStartNode = levelEventDatas.GetLast(true);
			selectionEndNode = levelEventDatas.GetNode(false);
			if (levelEventDatas.IsOutOfRange(false))
				selectionEndNode = levelEventDatas.GetLast(false);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(content, Input.mousePosition, null, out selectionStartPos);
			selection.gameObject.SetActive(true);
			//selectionStartPos = 
			//	new Vector2(selectionStartPos.x + content.rect.width / content.lossyScale.x, 
			//	selectionStartPos.y + content.rect.height / content.lossyScale.y);
			selection.localPosition = selectionStartPos;
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			isDragging = false;
			if (isDraggingEvents)
			{
				isDraggingEvents = false;
				levelEventDatas.Sort();

				if (!scrController.instance.paused) return;

				using (new SaveStateScope(scnEditor.instance))
				{

				}
			}
			if (showingSelection)
			{
				showingSelection = false;
				selection.gameObject.SetActive(false);

				if (!scrController.instance.paused) return;

				Vector2 startPos = selection.localPosition;
				Vector2 endPos =
					new Vector2(selection.localPosition.x + selection.sizeDelta.x,
					selection.localPosition.y - selection.sizeDelta.y);

				if (!RDInput.holdingControl && !RDInput.holdingShift)
					ClearSelect();

				if (startPos.x != endPos.x && startPos.y != endPos.y)
				{
					startPos.x += content.rect.width;
					endPos.x += content.rect.width;
					startPos.y = -startPos.y - 25f;
					endPos.y = -endPos.y - 25f;

					LinkedListNode<LevelEventData> startNode;
					LinkedListNode<LevelEventData> endNode;
					if (selectionStartPos != startPos)
					{
						startNode = levelEventDatas.GetNode(true);
						endNode = selectionEndNode;
					}
					else
					{
						startNode = selectionStartNode;
						endNode = levelEventDatas.GetNode(false);
					}
					if (startNode == null)
						startNode = levelEventDatas.GetLast(true);
					else if (startNode.Previous != null)
						startNode = startNode.Previous;
					if (endNode == null)
						endNode = levelEventDatas.GetLast(false);
					else if (endNode.Next != null)
						endNode = endNode.Next;

					HashSet<LevelEvent> alreadyProcessedEvent = new HashSet<LevelEvent>();

					Rect selectionRect = new Rect(startPos, endPos - startPos);

					while (startNode != null)
					{
						LevelEvent targetEvent = startNode.Value.evt;
						int row = 0;
						if (targetEvent.data.ContainsKey("row"))
							row = ETUtils.GetRowNumber(targetEvent);

						float eventStartPos = ETUtils.GetEventPosX(targetEvent);
						float eventEndPos = eventStartPos + ETUtils.GetEventObjWidth(targetEvent);
						float eventStartPosY = row * ETUtils.timelineHeight;
						float eventEndPosY = eventStartPosY + ETUtils.timelineHeight;

						Rect eventRect = new Rect(eventStartPos, eventStartPosY, eventEndPos - eventStartPos, eventEndPosY - eventStartPosY);

						if (selectionRect.Overlaps(eventRect, true))
						{
							if (!alreadyProcessedEvent.Contains(targetEvent))
							{
								if ((RDInput.holdingShift && !selectedEvents.Contains(targetEvent)) || !RDInput.holdingShift)
									ToggleSelectEvent(targetEvent, true, false);
								alreadyProcessedEvent.Add(targetEvent);
							}
						}
						else if (eventStartPos > endPos.x)
							break;
						startNode = startNode.Next;
					}

					while (endNode != null)
					{
						LevelEvent targetEvent = endNode.Value.evt;
						int row = ETUtils.GetRowNumber(targetEvent);

						float eventStartPos = ETUtils.GetEventPosX(targetEvent);
						float eventEndPos = eventStartPos + ETUtils.GetEventObjWidth(targetEvent);
						float eventStartPosY = row * ETUtils.timelineHeight;
						float eventEndPosY = eventStartPosY + ETUtils.timelineHeight;

						Rect eventRect = new Rect(eventStartPos, eventStartPosY, eventEndPos - eventStartPos, eventEndPosY - eventStartPosY);

						if (selectionRect.Overlaps(eventRect, true))
						{
							if (!alreadyProcessedEvent.Contains(targetEvent))
							{
								if ((RDInput.holdingShift && !selectedEvents.Contains(targetEvent)) || !RDInput.holdingShift)
									ToggleSelectEvent(targetEvent, true, false);
								alreadyProcessedEvent.Add(targetEvent);
							}
						}
						else if (eventEndPos < startPos.x)
							break;
						endNode = endNode.Previous;
					}
				}
				selectionStartNode = null;
				selectionEndNode = null;

				scnEditor editor = scnEditor.instance;
				if (selectedEvents.Count == 1)
				{
					editor.SelectFloor(editor.floors[selectedEvents.First().floor]);
					editor.levelEventsPanel.ShowPanelOfEvent(selectedEvents.First());
				}
				else if (selectedEvents.Count > 1)
				{
					DeselectFloors(editor, false, true);

					editor.levelEventsPanel.ShowInspector(true, true);
					editor.levelEventsPanel.ShowPanel(selectedEvents.First().eventType, 0);
				}
				else
				{
					DeselectFloors(editor, false, true);
				}
			}
		}

		public void OnDrag()
		{
			if (!scrController.instance.paused)
			{
				OnEndDrag(null);
				return;
			}

			if (isDraggingEvents)
			{
				// TODO: Magnet position and modify angle offset.
				scnEditor editor = scnEditor.instance;

				int startFloor = Mathf.Clamp(firstLineShowingOnScreenIdx, 0, editor.floors.Count - 1);
				scrFloor floor = editor.floors[startFloor];
				float contentX = GetLinePosX(floor);

				Vector2 mousePosition;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(scroll.viewport, Input.mousePosition, null, out mousePosition);

				MoveOnDrag(mousePosition, moveY: false);

				Vector2 contentPosition;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(content, Input.mousePosition, null, out contentPosition);

				contentPosition.x += content.rect.width;
				contentPosition.y = -contentPosition.y - 25f;

				while (floor.nextfloor != null)
				{
					if (GetLinePosX(floor) > contentPosition.x)
						break;
					contentX = GetLinePosX(floor);
					floor = floor.nextfloor;
				}
				floor = floor.prevfloor != null ? floor.prevfloor : floor;

				Vector2 targetPosition = contentPosition;
				Vector2 position = targetPosition - new Vector2(contentX, 0);

				float magnetX = ETUtils.timelineWidth / magnetNum;
				float magnetY = ETUtils.timelineHeight;
				int fixedX = (int)(position.x / magnetX);
				int fixedY = (int)(position.y / magnetY);

				//ETLogger.Debug($"floor: {floor.seqID}, dragOffset: {dragOffset}");

				foreach (var levelEvent in selectedEvents)
				{
					//if (RDInput.holdingAlt)
					//{

					//}
					//else
					//{
					levelEvent.floor = floor.seqID;
					if (levelEvent.data.ContainsKey("angleOffset"))
					{
						//float angleOffset = draggingEventDatas[levelEvent].angleOffset;
						//ETLogger.Debug($"fixedX: {fixedX}, magnetNum: {magnetNum}, speed: {floor.speed}");
						float angleOffset = fixedX / (float)magnetNum * floor.speed * 180f;
						levelEvent["angleOffset"] = angleOffset;
					}
					if (levelEvent.data.ContainsKey("duration"))
					{
						float duration = draggingEventDatas[levelEvent].duration;
						levelEvent["duration"] = duration * floor.speed;
					}
					//}
				}
				editor.ApplyEventsToFloors();
				Redraw();
			}
			if (showingSelection)
			{
				Vector2 position;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(content, Input.mousePosition, null, out position);
				//position = 
				//	new Vector2(position.x + content.rect.width / content.lossyScale.x, 
				//	position.y + content.rect.height / content.lossyScale.y);

				Vector2 size = position - selectionStartPos;
				size.y *= -1;

				Vector2 mousePosition;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(scroll.viewport, Input.mousePosition, null, out mousePosition);

				MoveOnDrag(mousePosition);

				selection.localPosition =
					new Vector2(size.x < 0 ? selectionStartPos.x + size.x : selectionStartPos.x,
					size.y < 0 ? selectionStartPos.y - size.y : selectionStartPos.y);

				if (size.x < 0)
					size.x *= -1;
				if (size.y < 0)
					size.y *= -1;

				selection.sizeDelta = size;
			}
		}

		private void MoveOnDrag(Vector2 mousePosition, bool moveX = true, bool moveY = true)
		{
			bool movedX = false;
			bool movedY = false;
			float x = 0f;
			float y = 0f;
			if (mousePosition.x < -scroll.viewport.rect.width / 2f + 30f)
			{
				movedX = true;
				x = Time.unscaledDeltaTime * 1600f / content.rect.width;
				x = scroll.normalizedPosition.x - x;
				x = Mathf.Clamp01(x);
			}
			else if (mousePosition.x > scroll.viewport.rect.width / 2f - 30f)
			{
				movedX = true;
				x = Time.unscaledDeltaTime * 1600f / content.rect.width;
				x = scroll.normalizedPosition.x + x;
				x = Mathf.Clamp01(x);
			}

			if (mousePosition.y < -scroll.viewport.rect.height / 2f + 30f)
			{
				movedY = true;
				y = Time.unscaledDeltaTime * 1600f / content.rect.height;
				y = scroll.normalizedPosition.y - y;
				y = Mathf.Clamp01(y);
			}
			else if (mousePosition.y > scroll.viewport.rect.height / 2f - 30f)
			{
				movedY = true;
				y = Time.unscaledDeltaTime * 1600f / content.rect.height;
				y = scroll.normalizedPosition.y + y;
				y = Mathf.Clamp01(y);
			}

			movedX = movedX && moveX;
			movedY = movedY && moveY;

			if (movedX || movedY)
			{
				scroll.normalizedPosition =
					new Vector2(movedX ? x : scroll.normalizedPosition.x,
					movedY ? y : scroll.normalizedPosition.y);
				OnValueChanged(scroll.normalizedPosition);
			}
		}

		public void DeselectFloors(scnEditor editor, bool clearSelection, bool skipSaving = false)
		{
			clearSelectedEventsAtDeselectFloor = clearSelection;
			editor.DeselectFloors(skipSaving);
			clearSelectedEventsAtDeselectFloor = true;
		}

		//public void UpdateSelectedEventPos(int seqID)
		//{
		//    if (selectedEvent == null || !selectedEvent.isRendering)
		//        return;
		//    scnEditor editor = scnEditor.instance;
		//    scrFloor floor = editor.floors[seqID];

		//    float position = TimeToBeat(floor.entryTime) * width * scale;

		//    object f;
		//    bool valueExist = selectedEvent.targetEvent.data.TryGetValue("angleOffset", out f);
		//    position += (valueExist ? (float)f : 0) / 180f * (1 / floor.speed) * width;
		//    selectedEvent.transform.LocalMoveX(position);
		//}

		//public void UpdateSelectedEventPos(float angleOffset)
		//{
		//    if (selectedEvent == null || !selectedEvent.isRendering)
		//        return;
		//    scnEditor editor = scnEditor.instance;
		//    scrFloor floor = editor.floors[selectedEvent.targetEvent.floor];

		//    float position = TimeToBeat(floor.entryTime) * width * scale;

		//    position += angleOffset / 180f * (1 / floor.speed) * width;
		//    selectedEvent.transform.LocalMoveX(position);
		//}

		//      public void AddNewEventObject(int floor, float x, int row, float entryTime)
		//      {
		//          float objWidth = height * scale;

		//	var eventData = new LevelEventData(entryTime, 0f, row, null);

		//	var obj = CreateEventObject(null, x, eventData.timelineRow, objWidth);

		//          eventData.obj = obj;

		//          if (selectingTargetEvent != null)
		//          {
		//              eventPool.Release(selectingTargetEvent.obj);
		//              selectingTargetEvent.obj = null;
		//          }

		//          selectingEventFloor = floor;
		//          selectingTargetEvent = eventData;
		//          NeoEditor.Instance.SetEventSelector();
		//}

		//public void ApplySelector(LevelEventType type)
		//{
		//          if (selectingEventFloor < 0) return;
		//          NeoEditor editor = NeoEditor.Instance;
		//          var levelEvent = editor.AddEvent(selectingEventFloor, type);

		//          // craete new LevelEventData and copy gameobject
		//          var eventData = new LevelEventData(selectingTargetEvent.start, 0f, selectingTargetEvent.timelineRow, levelEvent);
		//          eventData.obj = selectingTargetEvent.obj;

		//          // change TimelineEvent gameobject icon
		//          var timelineEvent = eventData.obj.GetComponent<TimelineEvent>();
		//          timelineEvent.transform.GetChild(0).GetComponent<Image>().sprite = GCS.levelEventIcons[levelEvent.eventType];
		//          timelineEvent.transform.GetChild(0).gameObject.SetActive(true);
		//	timelineEvent.GetComponent<Image>().color = NeoConstants.EventColors[levelEvent.eventType];
		//	timelineEvent.targetEvent = levelEvent;

		//          levelEventsDataSortedByStartPos.Add(eventData);
		//	levelEventsDataSortedByEndPos.Add(eventData);

		//	levelEventsDataSortedByStartPos.InsertionSort(
		//		(a, b) =>
		//		{
		//			// sort by event start position x, smaller one goes first
		//			var diff = GetEventPosX(a.evt) - GetEventPosX(b.evt);
		//			if (diff < 0)
		//				return -1;
		//			else if (diff > 0)
		//				return 1;
		//			else
		//				return 0;
		//		}
		//	);

		//	levelEventsDataSortedByEndPos.InsertionSort(
		//		(a, b) =>
		//		{
		//			var aStartPosX = GetEventPosX(a.evt);
		//			var aObjWidth = GetEventObjWidth(a.evt);
		//			var aEndPosX = aStartPosX + aObjWidth;

		//			var bStartPosX = GetEventPosX(b.evt);
		//			var bObjWidth = GetEventObjWidth(b.evt);
		//			var bEndPosX = bStartPosX + bObjWidth;

		//			// sort by event end position x, smaller one goes first
		//			float diff = aEndPosX - bEndPosX;
		//			if (diff < 0)
		//				return -1;
		//			else if (diff > 0)
		//				return 1;
		//			else
		//				return 0;
		//		}
		//	);
		//	levelEventsSortedByStartPosListEndIdx++;

		//          selectingEventFloor = -1;
		//          selectingTargetEvent = null;

		//          SelectEvent(timelineEvent);
		//}

		//public void OnPointerClick(PointerEventData eventData)
		//      {
		//          NeoEditor editor = NeoEditor.Instance;
		//          var floors = editor.floors;

		//          Vector2 localPos;
		//          RectTransformUtility.ScreenPointToLocalPointInRectangle(
		//              content,
		//              eventData.position,
		//              null,
		//              out localPos
		//          );

		//          //find floor
		//          float posX = -1f;
		//          int floor = -1;
		//          VerticalLineData prevLine = vLines.First.Value;
		//          foreach (VerticalLineData line in vLines)
		//          {
		//              if (localPos.x < line.x)
		//              {
		//                  posX = prevLine.x;
		//                  floor = prevLine.id;
		//                  break;
		//              }
		//              prevLine = line;
		//          }

		//          if (floor == -1)
		//              return;

		//          int posY = Mathf.FloorToInt(-localPos.y / (height * scale));

		//          AddNewEventObject(floor, posX, posY, (float)floors[floor].entryTime);
		//      }
	}
}
