using ADOFAI;
using EditorTweaks.Patch.Timeline;
using EditorTweaks.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditorTweaks.Components
{
	public class LevelEventDataCollection : Dictionary<LevelEvent, LevelEventData>
	{
		public LinkedList<LevelEventData> sortedByStartPos = new LinkedList<LevelEventData>();
		public LinkedList<LevelEventData> sortedByEndPos = new LinkedList<LevelEventData>();

		private LinkedListNode<LevelEventData> viewportStart;
		private LinkedListNode<LevelEventData> viewportEnd;

		//private List<LevelEventData> levelEventData = new List<LevelEventData>();

		private bool viewportStartInitialized = false;
		private bool isViewportStartOutOfRange => viewportStartInitialized && Count > 0 && viewportStart == null;
		private bool startIsMinus { get; set; }

		private bool viewportEndInitialized = false;
		private bool isViewportEndOutOfRange => viewportEndInitialized && Count > 0 && viewportEnd == null;
		private bool endIsMinus { get; set; }

		public new LevelEventData this[LevelEvent key]
		{
			get => base[key];
			set
			{
				base[key] = value;

				Sort();
			}
		}

		public new void Add(LevelEvent key, LevelEventData value)
		{
			Add(key, value, true);
		}

		public void Add(LevelEvent key, LevelEventData value, bool sort = true)
		{
			base.Add(key, value);

			sortedByStartPos.AddLast(value);
			sortedByEndPos.AddFirst(value);

			//levelEventData.Add(value);

			if (!sort)
				return;
			if (viewportStart == null && !isViewportStartOutOfRange)
			{
				viewportStartInitialized = true;
				viewportStart = GetLast(true);
			}
			if (viewportEnd == null && !isViewportEndOutOfRange)
			{
				viewportEndInitialized = true;
				viewportEnd = GetLast(false);
			}

			Sort();
		}

		public new void Clear()
		{
			base.Clear();

			sortedByStartPos.Clear();
			sortedByEndPos.Clear();

			//levelEventData.Clear();

			viewportStart = null;
			viewportEnd = null;
		}

		public new bool Remove(LevelEvent key)
		{
			LinkedListNode<LevelEventData> shouldRemoveFromEndPos = sortedByEndPos.Find(this[key]);
			LinkedListNode<LevelEventData> shouldRemoveFromStartPos = sortedByStartPos.Find(this[key]);
			if (viewportStart != null && shouldRemoveFromEndPos == viewportStart)
				Move(true, true);
			if (viewportEnd != null && shouldRemoveFromStartPos == viewportEnd)
				Move(false, true);

			sortedByStartPos.Remove(this[key]);
			sortedByEndPos.Remove(this[key]);

			//levelEventData.Remove(this[key]);

			bool result = base.Remove(key);

			Sort();
			return result;
		}

		public LinkedListNode<LevelEventData> GetNode(bool startPos)
		{
			return startPos ? viewportStart : viewportEnd;
		}

		public void Move(bool startPos, bool moveNext)
		{
			if (startPos)
			{
				moveNext = !moveNext;
				bool wasOutOfRange = false;
				bool startWasMinus = false;
				if (isViewportStartOutOfRange)
				{
					if (startIsMinus && moveNext)
					{
						wasOutOfRange = true;
						startWasMinus = true;
						viewportStart = sortedByEndPos.First;
					}
					else if (!startIsMinus && !moveNext)
					{
						wasOutOfRange = true;
						startWasMinus = false;
						viewportStart = sortedByEndPos.Last;
					}
				}

				if (viewportStart != null)
					viewportStart = moveNext ? viewportStart.Next : viewportStart.Previous;
				if (viewportStart == null)
				{
					if (wasOutOfRange)
						startIsMinus = startWasMinus;
					else
						startIsMinus = !moveNext;
				}
			}
			else
			{
				bool wasOutOfRange = false;
				bool endWasMinus = false;
				if (isViewportEndOutOfRange)
				{
					if (endIsMinus && moveNext)
					{
						wasOutOfRange = true;
						endWasMinus = true;
						viewportEnd = sortedByStartPos.First;
					}
					else if (!endIsMinus && !moveNext)
					{
						wasOutOfRange = true;
						endWasMinus = false;
						viewportEnd = sortedByStartPos.Last;
					}
				}

				if (viewportEnd != null)
					viewportEnd = moveNext ? viewportEnd.Next : viewportEnd.Previous;
				if (viewportEnd == null)
				{
					if (wasOutOfRange)
						endIsMinus = endWasMinus;
					else
						endIsMinus = !moveNext;
				}
			}
		}

		public void SetIdx(bool startPos, LinkedListNode<LevelEventData> node)
		{
			if (startPos)
			{
				viewportStart = node;
				if (viewportStart == null)
					startIsMinus = false;
			}
			else
			{
				viewportEnd = node;
				if (viewportEnd == null)
					endIsMinus = true;
			}
		}

		public bool IsOutOfRange(bool startPos)
		{
			return startPos ? isViewportStartOutOfRange : isViewportEndOutOfRange;
		}

		public bool IsMinus(bool startPos)
		{
			return startPos ? startIsMinus : endIsMinus;
		}

		public LinkedListNode<LevelEventData> GetFirst(bool startPos)
		{
			return startPos ? sortedByEndPos.Last : sortedByStartPos.First;
		}

		public LinkedListNode<LevelEventData> GetLast(bool startPos)
		{
			return startPos ? sortedByEndPos.First : sortedByStartPos.Last;
		}

		//public void FirstSort()
		//{
		//	Sort();

		//if (sortedByStartPos != null || sortedByStartPos.Count > 1)
		//{
		//	stack = 0;
		//	QuickSort(sortedByStartPos.First, sortedByStartPos.Last, (a, b) =>
		//	{
		//		// sort by event start position x, smaller one goes first
		//		var diff = ETUtils.GetEventPosX(a.evt) - ETUtils.GetEventPosX(b.evt);
		//		if (diff < 0)
		//			return -1;
		//		else if (diff > 0)
		//			return 1;
		//		else
		//			return 0;
		//	});
		//	viewportEnd = null;
		//	EndIsMinus = true;
		//}

		//if (sortedByEndPos != null || sortedByEndPos.Count > 1)
		//{
		//	stack = 0;
		//	QuickSort(sortedByEndPos.First, sortedByEndPos.Last, (a, b) =>
		//	{
		//		var aStartPosX = ETUtils.GetEventPosX(a.evt);
		//		var aObjWidth = ETUtils.GetEventObjWidth(a.evt);
		//		var aEndPosX = aStartPosX + aObjWidth;

		//		var bStartPosX = ETUtils.GetEventPosX(b.evt);
		//		var bObjWidth = ETUtils.GetEventObjWidth(b.evt);
		//		var bEndPosX = bStartPosX + bObjWidth;

		//		// sort by event end position x, smaller one goes first
		//		float diff = aEndPosX - bEndPosX;
		//		if (diff < 0)
		//			return 1;
		//		else if (diff > 0)
		//			return -1;
		//		else
		//			return 0;
		//	});
		//	viewportStart = null;
		//	StartIsMinus = false;
		//}
		//}

		public void Sort()
		{
			//levelEventData.Sort((a, b) =>
			//{
			//	// sort by event start position x, smaller one goes first
			//	var diff = ETUtils.GetEventPosX(a.evt) - ETUtils.GetEventPosX(b.evt);
			//	if (diff < 0)
			//		return -1;
			//	else if (diff > 0)
			//		return 1;
			//	else
			//		return 0;
			//});

			InsertionSort(sortedByStartPos, (a, b) =>
			{
				// sort by event start position x, smaller one goes first
				var diff = ETUtils.GetEventPosX(a.evt) - ETUtils.GetEventPosX(b.evt);
				if (diff < 0)
					return -1;
				else if (diff > 0)
					return 1;
				else
					return 0;
			});

			InsertionSort(sortedByEndPos, (a, b) =>
				{
					var aStartPosX = ETUtils.GetEventPosX(a.evt);
					var aObjWidth = ETUtils.GetEventObjWidth(a.evt);
					var aEndPosX = aStartPosX + aObjWidth;

					var bStartPosX = ETUtils.GetEventPosX(b.evt);
					var bObjWidth = ETUtils.GetEventObjWidth(b.evt);
					var bEndPosX = bStartPosX + bObjWidth;

					// sort by event end position x, smaller one goes first
					float diff = aEndPosX - bEndPosX;
					if (diff < 0)
						return -1;
					else if (diff > 0)
						return 1;
					else
						return 0;
				}
			);

			if (viewportStart == null && !isViewportStartOutOfRange)
			{
				viewportStartInitialized = true;
				viewportStart = GetLast(true);
			}
			if (viewportEnd == null && !isViewportEndOutOfRange)
			{
				viewportEndInitialized = true;
				viewportEnd = GetLast(false);
			}
		}

		private void InsertionSort(LinkedList<LevelEventData> list, Comparison<LevelEventData> comparison)
		{
			LinkedListNode<LevelEventData> current;
			current = list.First;

			while (current != null)
			{
				LinkedListNode<LevelEventData> current2 = current;

				while (current2.Previous != null && comparison(current2.Previous.Value, current2.Value) > 0)
				{
					Swap(current2.Previous, current2);
					current2 = current2.Previous;
				}

				current = current.Next;
			}

			void Swap(LinkedListNode<LevelEventData> left, LinkedListNode<LevelEventData> right)
			{
				if (left.List != right.List)
				{
					ETLogger.Warn("left and right are not same list.");
					return;
				}

				if (left == right)
				{
					ETLogger.Debug("left and right are same node.");
				}

				bool isSortedByEndPos = left.List == sortedByEndPos;
				LinkedListNode<LevelEventData> targetPointer = isSortedByEndPos ? viewportStart : viewportEnd;

				bool leftIsPointer = left == targetPointer;
				bool rightIsPointer = right == targetPointer;

				if (leftIsPointer)
				{
					if (isSortedByEndPos)
						viewportStart = right;
					else
						viewportEnd = right;
				}
				else if (rightIsPointer)
				{
					if (isSortedByEndPos)
						viewportStart = left;
					else
						viewportEnd = left;
				}

				var temp = left.Value;
				left.Value = right.Value;
				right.Value = temp;
			}
		}
	}
}
