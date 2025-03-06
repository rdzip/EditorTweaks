using DG.Tweening;
using EditorTweaks.ADOFAI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTweaks.Utils
{
	public static class ETUtils
	{
		public static GameObject FindOrCreatePrefabsObject()
		{
			GameObject obj = GameObject.Find("ET_Prefabs");
			GameObject inactiveObj;
			if (obj == null)
			{
				obj = new GameObject("ET_Prefabs");
				GameObject.DontDestroyOnLoad(obj);
				inactiveObj = new GameObject("Inactives");
				inactiveObj.transform.SetParent(obj.transform, false);
				inactiveObj.SetActive(false);
			}
			else
			{
				inactiveObj = obj.transform.Find("Inactives").gameObject;
			}
			return inactiveObj;
		}

		public static ScrollRect CreateScrollRect(Transform parent, bool horizontal, bool vertical)
		{
			GameObject scrollRectObj = new GameObject("scrollRect");
			scrollRectObj.transform.SetParent(parent, false);

			RectTransform scrollRectRT = scrollRectObj.AddComponent<RectTransform>();
			scrollRectRT.anchorMin = Vector2.zero;
			scrollRectRT.anchorMax = Vector2.one;
			scrollRectRT.offsetMin = new Vector2(4f, 0f);
			scrollRectRT.offsetMax = new Vector2(-4f, -16f);

			GameObject viewportObj = new GameObject("viewport");
			viewportObj.transform.SetParent(scrollRectRT, false);

			RectTransform viewportRT = viewportObj.AddComponent<RectTransform>();
			viewportRT.anchorMin = Vector2.zero;
			viewportRT.anchorMax = Vector2.one;
			viewportRT.offsetMin = Vector2.zero;
			viewportRT.offsetMax = Vector2.zero;
			Image viewportImage = viewportObj.AddComponent<Image>();
			Mask viewportMask = viewportObj.AddComponent<Mask>();
			viewportMask.enabled = true;
			viewportMask.showMaskGraphic = false;

			GameObject contentObj = new GameObject("content");
			contentObj.transform.SetParent(viewportRT, false);

			RectTransform contentRT = contentObj.AddComponent<RectTransform>();
			contentRT.anchorMin = Vector2.zero;
			contentRT.anchorMax = Vector2.zero;
			contentRT.anchoredPosition = Vector2.zero;
			contentRT.pivot = Vector2.one;

			Scrollbar horizontalBar = CreateScrollbar(viewportRT, true, 10f);
			Scrollbar verticalBar = CreateScrollbar(viewportRT, false, 10f);

			ScrollRect scrollRect = scrollRectObj.AddComponent<ScrollRect>();
			scrollRect.content = contentRT;
			scrollRect.horizontal = horizontal;
			scrollRect.vertical = vertical;
			scrollRect.movementType = ScrollRect.MovementType.Clamped;
			scrollRect.inertia = true;
			scrollRect.scrollSensitivity = 0f;
			scrollRect.viewport = viewportRT;
			scrollRect.horizontalScrollbar = horizontalBar;
			scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
			scrollRect.verticalScrollbar = verticalBar;
			scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

			SmoothScrollRectForTimeline smoothScrollRect = scrollRectObj.AddComponent<SmoothScrollRectForTimeline>();
			smoothScrollRect.ease = Ease.OutCubic;
			smoothScrollRect.enabled = true;
			smoothScrollRect.scrollRect = scrollRect;
			smoothScrollRect.scrollSensitivity = 60f;
			smoothScrollRect.scrollSensitivityHorizontal = 160f;
			smoothScrollRect.tweenDuration = 0.3f;

			return scrollRect;
		}

		public static Scrollbar CreateScrollbar(Transform parent, bool direction, float size)
		{
			GameObject scrollbarObj = new GameObject("scrollbar_" + (direction ? "horizontal" : "vertical"));
			scrollbarObj.transform.SetParent(parent, false);

			RectTransform scrollbarRT = scrollbarObj.AddComponent<RectTransform>();

			if (direction)
			{
				// Horizontal.
				scrollbarRT.anchorMin = Vector2.zero;
				scrollbarRT.anchorMax = Vector2.right;
				scrollbarRT.pivot = Vector2.zero;
				scrollbarRT.offsetMax = new Vector2(-size, 0f);
				scrollbarRT.offsetMin = Vector2.zero;
				scrollbarRT.SizeDeltaY(size);
				scrollbarRT.localPosition = Vector3.zero;
				scrollbarRT.anchoredPosition = Vector2.zero;
			}
			else
			{
				// Vertical.
				scrollbarRT.anchorMin = Vector2.right;
				scrollbarRT.anchorMax = Vector2.one;
				scrollbarRT.pivot = Vector2.one;
				scrollbarRT.offsetMax = new Vector2(0f, -size);
				scrollbarRT.offsetMin = Vector2.zero;
				scrollbarRT.SizeDeltaX(size);
				scrollbarRT.localPosition = Vector3.zero;
				scrollbarRT.anchoredPosition = Vector2.zero;
			}

			GameObject slidingAreaObj = new GameObject("slidingArea");
			slidingAreaObj.transform.SetParent(scrollbarRT, false);
			
			RectTransform slidingAreaRT = slidingAreaObj.AddComponent<RectTransform>();
			slidingAreaRT.anchorMin = Vector2.zero;
			slidingAreaRT.anchorMax = Vector2.one;
			slidingAreaRT.offsetMin = new Vector2(size, size);
			slidingAreaRT.offsetMax = new Vector2(-size, -size);
			slidingAreaRT.localPosition = Vector3.zero;
			slidingAreaRT.anchoredPosition = Vector2.zero;

			GameObject handleObj = new GameObject("handle");
			handleObj.transform.SetParent(slidingAreaRT, false);

			RectTransform handleRT = handleObj.AddComponent<RectTransform>();
			handleRT.offsetMin = new Vector2(-size, -size);
			handleRT.offsetMax = new Vector2(size, size);

			Image handleImage = handleObj.AddComponent<Image>();
			handleImage.sprite = FindUISprite();
			handleImage.type = Image.Type.Sliced;
			handleImage.fillCenter = true;
			handleImage.pixelsPerUnitMultiplier = 1f;

			Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
			scrollbar.interactable = true;
			scrollbar.transition = Selectable.Transition.ColorTint;
			scrollbar.targetGraphic = handleImage;
			ColorBlock colors = new ColorBlock();
			colors.normalColor = Color.white.WithAlpha(0.6f);
			colors.highlightedColor = new Color(0.9607843f, 0.9607843f, 1f, 0.8f);
			colors.pressedColor = new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f);
			colors.selectedColor = new Color(0.9607843f, 0.9607843f, 1f, 0.8f);
			colors.disabledColor = new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.5f);
			colors.colorMultiplier = 1f;
			colors.fadeDuration = 0.1f;
			scrollbar.colors = colors;
			scrollbar.navigation = Navigation.defaultNavigation;
			scrollbar.handleRect = handleRT;
			scrollbar.direction = direction ? Scrollbar.Direction.LeftToRight : Scrollbar.Direction.BottomToTop;

			return scrollbar;
		}

		private const string unityBuiltin_UISprite_base64 = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAACXBIWXMAAA7EAAAOxAGVKw4bAAACq0lEQVR4nO1XPYgaURAe3fW3iYVdtAiciEIgaGcjBAKmCoIggt1B4MAf8CcgpDwIBKKIgiBcJ4ooaIrAQSCQUkQJHATELtjYB/9/MrPxLc/DwGmUa/xgGN3Zne97896+fSPa7XbgodVqQalUSr9Xq5Xk1+s1KBQK2Af8M3y+yWSydZ+o0WhgOp0C8/TgcrkEQRDgmKCcDDRIEkJeJCK9Xg+j0UgKcsRWtLdobrQXFNqXE+0H2ne0IubtsQCREydBJGV8BTZKY2jXsVhM53Q6wWq1ymV8KLDcQq/Xc3Y6HWc6nb7CS+/R0hQjLhoweZFICRuvxnn77PP5PMFgEMxmMxwKEmyz2SRzu926Uqn0qV6vv8LQG+SaMU5pDXD4GAgEPIlEAo4JGkgqlQKVSuWpVCof8FKcxUSutM+xbKFoNCr9oRW7WCwkz96GfUG5yURRlHw4HIZyuRzFhX6D4Z+SAC75ZTKZFFClRDybzeB/wcRTPrVaLc05rishk8lcwqYKIne/x+Vy8QvxqKABUV7iQAEeWQC3wTwzGo0nIWeg3AaDgfaap+waXwH1vrvdIaDNB3me7BLwKDgLOAs4CzgLOAvgBczm87maPsenxHg8Jvd7l4Bfw+HwwmQynVQAckhcuwTctlqt0KkFtNtt+hx/lQVwn+AiHhSuvF6vsO8J+KHAKYZsNrvE/DeyAGpENrhDMYV8Ph+KRCInEVAoFMjl8Zh2Jwu41yrFa7XaBSr1+P1+ONZ0DAYDqFar0Gw2b/HI946P3W/NZliR141GI4Z2jadYncPhAIvFchBxv9+HbrcLuVyOlv5WY8I4/9Wa0Y1fcDqoNXsJf1uzQ0Ct2TfMWUQvt2ZELrdmrGNlDSM7lG56uTgcCfxhd6s1Y2uAeXorTvEWUGVZD8K3g38A8BxHBF/VD5oAAAAASUVORK5CYII=";
		private static Sprite unityBuiltin_UISprite;
		public static Sprite FindUISprite()
		{
			if (unityBuiltin_UISprite == null)
			{
				Texture2D texture = new Texture2D(32, 32);
				texture.LoadImage(Convert.FromBase64String(unityBuiltin_UISprite_base64));

				unityBuiltin_UISprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 200f, 0, SpriteMeshType.FullRect, new Vector4(10f, 10f, 10f, 10f));
			}
			return unityBuiltin_UISprite;
		}

		public const string timelineIcon_base64 = "iVBORw0KGgoAAAANSUhEUgAAAIAAAACACAYAAADDPmHLAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAFXSURBVHgB7d3BUYRAEIbRxgg0AzPTFMxgMzAGI1sycDPA4WbpaZ1xgf3fq5riDPVxo5sqAAAAAAAAAODeTNVpWZbXdnlv57FYze2cpmn6qAMYEcC5XZ6L7y4tgKc6gBEBLMUvLYDuZ3sLD0U0AYQTQDgBhBNAOAGEE0A4AYQTQDgBhBNAuBEBXIqf5jqIEQG81YFu+AbWF+JUAAAAAAAAAAAb2XSC1W6BLnMN2EOwdQB2C/Tp3kOwdQB2C3Tq3UPgq+BwAggngHACCCeAcAIIJ4BwAggngHACCCeAcFsHYLdAn7k6bR2A3QJ/Zw8BAAAAAAAAAPA/1hn/dj6XbZ3beSmuNuL38XuZ8e+elU80IoDdzPj3zson8lVwOAGEE0A4AYQTQDgBhBNAOAGEE0A4AYQTQLgRAexlxn8urjYigD3M+JuVBwAAAAAAAAAAAAAAAAAAAO7WFyHLvw8KinumAAAAAElFTkSuQmCC";
		private static Sprite timelineIcon;

		public static Sprite LoadTimelineIcon()
		{
			if (timelineIcon == null)
			{
				Texture2D texture = new Texture2D(32, 32);
				texture.LoadImage(Convert.FromBase64String(timelineIcon_base64));

				timelineIcon = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
			}
			return timelineIcon;
		}
	}
}
