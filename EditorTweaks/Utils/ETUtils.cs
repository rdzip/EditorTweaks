using ADOFAI;
using DG.Tweening;
using EditorTweaks.ADOFAI;
using EditorTweaks.Components;
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

			ScrollRect scrollRect = scrollRectObj.AddComponent<NoDragScrollRect>();
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
			smoothScrollRect.scrollSensitivity = 160f;
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

		public const string selectionRect_base64 = "iVBORw0KGgoAAAANSUhEUgAAAAMAAAADCAYAAABWKLW/AAAAAXNSR0IArs4c6QAAABpJREFUCJlj/P///38GKGCC0o0MDAwMjMgyAMTlCH52SsY3AAAAAElFTkSuQmCC";
		private static Sprite selectionRect;

		public static Sprite LoadSelectionRect()
		{
			if (selectionRect == null)
			{
				Texture2D texture = new Texture2D(3, 3);
				texture.LoadImage(Convert.FromBase64String(selectionRect_base64));
				texture.filterMode = FilterMode.Point;

				selectionRect = Sprite.Create(texture, new Rect(0, 0, 3, 3), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(1f, 1f, 1f, 1f));
			}
			return selectionRect;
		}

		public const string magnetIcon_base64 = "iVBORw0KGgoAAAANSUhEUgAAAIAAAACACAYAAADDPmHLAAAACXBIWXMAACxLAAAsSwGlPZapAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAzNSURBVHgB7Z09kCU3EYB7tgixcWoc4OzIcIDztTMSHxEJriLFEY6cuYrCGSSOOAccDsAERBiqCH1FbAeQcQkm8ZHYGDApT6hrR2xfv+5Wa56k0Rz6qqZmnkaaN5Ja/TfzdgEmk8lkMplMJpPJZDKZTCaTyWQymTzJLNCBEMIzcfdC3L4Rt2fKWsOf4vbHZVn+Cgci9vn5uLuO29egnD/ATZ//AUcGJz5uP4zb5+GWk7A/hTzvroM6NPEer+P2QOivl1T/83XsShfMGOBkxe1jMsEno8OnzICk83+P2/dgUNYJC6w/J6XMGg/KX8IBBP8x2OSHUL4KgjKA6fgaBiPe0+uZ+5c+e4UCheA4miDcqOugdO6UGRReJtX/eKQBCTcCfwnaQqGf34UjwAYjJ+2nzEBYgvEDGIRwLvDBuG++GLztkOpCfwX1uRu3sB7zKCOQYykCWTKf6XW+DeNwve6DsHEWsMdFapPKqvs/LQQAwz3soNZ5vg/gJ8DtAL4AA7CuShrqLXA+yQFsgV9YmbZQqve5hQA8D7JEL0IZLQ+gaw5aN9UZxQfA+0j3ZU0ewPm4BKE8911VaSEAyCIc084ujnYULhwlWqMXfDVLk8vHRWqjLR48/gpU5kvQDroqpHIAOxNJ63jU6d7w+9L6z9sAyKaAX7dJn1tpgIRnspPUS4IimY0RJx+RNFNu8rUJp+0lbVqN1gKQkDxiKvWaf+AxGyORhEDqr1SW2gDYkVOpr+CmlwCkSc45hACPDyJXoyPa/oS1cqUyOh5W6Hd4DaBNntVp2m5x1B0BS1i1iZa0YFdT11oANA9YW92aMyQ5VCOZBClCkTSeFSFYE9+srz2cwIQ0wdaE8kHi1xpJAyyOY+oblDi3TfvZUgC488IdpNwkSqte0yij4DFrXDNoJo62aUZLAdAcIb63HCHNLo44+UhOffN+SBqB1gNWrzq9ogBEU3+5sMcSkJHIObl0tVshn4S0mKrQSgC0zkuOkRbu5MKq0ZDu2aqrtfMcV6PHs4AE70xa/YtxDSk8AuXznnDP/5KJ6yroPUyAZL81xw7AHoBRnUDJscth+TxS9NCkvy0eBn0Wt0fQnlE0wH/gtr+SGah5n59BZVoIwD/j9inIyR6Ac3Up1ckxkvpHAfhUOcf7pyW+pP7j5xOrU/13Aq3DQLqXyi2VZ9nR0R1CjjfcpT4EnXyAg0UBOTTPlzqGuc4fQQjS+PJ+aVpRCw+b0UIAwobzmlM4kqq34BqLlnFVD+CLkrrQwgewcto5D3nJ1G3mDV+I5z61Sfdou2b00gDUCeJ1LPW+OOqOgOWj8NUfjHPStZr2v1cmMMGdvlyqlwqOVW9vpIkEo0xrz4+3XKuIFgIgqW5L/Ul1PEmSkQVBK7NW9y79aflWcA6PL0A/4wDtFbXUwloYXFCkaOHwJiA42mirIg3Iad2OiseR5dqOtq1K64dBJfFtTn16yvdCs/2a45fzFbr5Oz3fCs7ZO01IPP7E3mipbm0l566hZUir972VD+BZ8by+lT84SigoOcAl7QF8DnM1WpoAS3oXpb6lNqVrjkRJ+CZpQ0t4mgl9zzeCaLm0ok/ss3Qden5UTSBRcq90jLj2O5wTqJXT79VWh7YiujlIhZQkgXjKWzqXji3NeTF7PAyS6uTsn8RoGkAyYQvoApuz64vQ7jCZQA+SjU/HfBCPgtQPj10Pyp5ep4nGa+kDSDft7US6r0W45hGQNJo11ouyp+ebLIRWYaAl9Txcwv2JlJ0KrncUrAddJRzCB0hoto/bthPI9s66hla2F0vBZ49mzGUWq9HzbwRpdXKDxwnOej3hWstK60o5DboAAsgL4jCJIKrSt8InmQ7ElVC2N7lQTqoDSh1t8TRxiHuEgVvCGO5B8xVCy0ZA8ughU0Y5GXW5VqhKr0QQnTRLIPgEn9g1R3UGJVVdor41c4Bcge4jXUxrDSCtVi4E1vmjhYFW/qL03ptNOqWFANBraqtWUu3e+iMLgmXvrcQPP+Y0628PDcDTo7yO1n4R2qfyUU1BCZYW5OcP9zQwkSaLZ/ZynjFPDkGm/kh4kj5SKMjb57RIFXokgpCTow5nKaw/CpqPo917ThNIEVA1evw4lB9vITdIoyFpOG9mVCs/VBhYkgvQQkEpD5DOj/hWMJ8wyXdBtHchLo0aNtErD2DFuVJdLdtn2c49kdSzpPYlodAc5EMLAEVb8UtBm1S/6+AUoJk7abVrqj7nIDfpcw8TYGkDCysJZDlVeyGtYF7GtVcwrsPLDpcKrjFJXUOiC5D8FSnso/V5HVDKsG4zTd0rDJQ+e7CE6EgPg6z6nrqHygRSrFAw1ykeCUjnR2RE86Sy169t08RqjlE6f5TJvyRPESrV2cQefyXMquN1lkZbZR7tpuVCPH055LOALSGb5vlr6dUReGrdSwksbaI9kdGh8wC5FeCdvCurTQjhq7A/KADUZF2Brsm8aEJUnZ4/Ddtiz3PnnoL9wXvg/oxmvnLjzTUEv9YhngZ+IpRpGqE0wcHj7Rdhf9I9eEJBj623NOUnUJmefyBCKvPYPsthugP7g/cgZfyoD0OR/IAt5rEKLQTgC6iLtbKuox+wmxlYfZBvgh7xWKYw91AIhM+1x7aJAHj/VLwnw5dTiU/DzQTshfXdOdudeygktf03VKaFAPCbvCTR4fmByXdhP14TyqyHQhRqAnNRU+LPUJkWAsBvUptAj63zRBMvRlX8CnQmficKHpoA/tTPeih0dhmjrpRcqv6POHqYAJogoWUUa9B4W+ltoDd65gTW73p1/UhXsicS8AiE2C7yECpTXQDiTaKjQoVgYRtAPicQhPbaZ+TLcXsb+vFzuFn9iDXR0rkTnGsNqS2n+uQjrcLAD9Y9HwSPP5CrK9lU/J47cWX+CBoTv+OtuHuWfXdJSLeAvChy2b/q9h9pJQBJWksTPUDaJUps6t04QW+3MAcYbsbtfjzk/gYXyJwZ0MyZpjESD6ABrQTAe7PWEzK+GjTzwLmO2/04WdXCw/Vav4bHY356D1rsLiWErIdC4tev+4+gAU0EYPUD6A17zIDlAWvnNPuJGgCF4K1LtAFO/Lrq74Nt86X7TRO9gN0f6VpcYD5ax7Q6Lf9cPPoB0iq0JjrB438rH8DNDP2M6vqVOIkfxv3v4vZhHEgzlFoF5qW4vQyXJZlKzB+3/7zd+9CILfbZxZqi/T3cZOv+VwxyfFt6H9iGPnYtuQ6upIfr/gu4far4HNys8pqp5a19o20eRaH9FjSimQZAlRWF4Ffx8Pu0WKoKGy4P53bWex2cYL6ycytQK6ffLT3dzN2TJMi8Px9AQ1o/DfwlbH+AEZzncjG15xq5yc9dg/6dY80hlNA8fzov70FDmgrA6rikDlg23IOUZgX22etVa6bIwvoOes6KEnhkw4952b2cz3IpPd4HSFogrRLvypYcO08EkYMnYGg5OO4td/9auErP5TKbCE78b6ExzQVg1QL3IL96zctkzpeoft5GE7rcOX4+kWz6IpR7tSDWa776kS5vBMWOoBmQEhlSx+neosRH4NeHTJnnnHZe8yPSMwCLdP79OGbNVz/S84chb8K5Q5hbETmHSmsveeIBfHY+OOvSa3vIaZAkOLjq70EnSjpwMTEsxBcof8aL4fGVH4zzPbDCvZbfkfhOi8e+Gl1/GhY7hhk5Lt3c2QP2+ZLJD4Xll1ByTS5Q6fjHPScf6f7bwNjBd+ION57ISeRstnhZxzE1K5qmsdpY3wmgawlP5JKcvqYxf+4muhLNAWYIX8tU806U+jXgf4bgaeM5z+F9kLKG99aF0Z3dBABRhKC3zU9ISRpKi/vC6/10r8lPN7Ar68uVb5Q0gXorlNe1cvK0vqU9eG4flPr/ittPeoV7GrsLABKFAH9dg+/0XfImj/SAqLQ9/+8lHhMhfa8lKFj2t7i93tvhkxhCAJD1OTyahLv8FOTzBXzStmgB7hxKK58fa9eyzmFq/J1WL3iUMowAJNZ3/FEQnqPFcKtat9pir1bwOpmlWgYTPG/GiW/yatdWhhMAZNUG6Bu8yk9BPnW7RfV722jCYSWz0NZjePfeKKueMqQAJFZBwCgBzUKpfZfCrbOvAF3tS/VKwJ/I/QIGnfjE0AKQIP4BppLxnfz0CJUnd86aCnUBtmkKLzjZaOeHnvjEIQSAEoXhJbh52fPlVARl/fCEkdTfALBVPoKr/TdxezCajc9xOAFIrC+d4rt9KBBfh7I/FuFR+dJ5qjnwlzo42YebdMphBYCzmok764YC8ey6N5uBHt6licbVjR48TvjDtB1BvXt4YgRAYxUMDCnDuscfkj4Ncs7g0fo5/cD10ZMy0ZPJZDKZTCaTyWQymUwmk8lkMplM/j/5L74L8IU/QbNtAAAAAElFTkSuQmCC";
		private static Sprite magnetIcon;

		public static Sprite LoadMagnetIcon()
		{
			if (magnetIcon == null)
			{
				Texture2D texture = new Texture2D(3, 3);
				texture.LoadImage(Convert.FromBase64String(magnetIcon_base64));

				magnetIcon = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
			}
			return magnetIcon;
		}

		public const string playheadIcon_base64 = "iVBORw0KGgoAAAANSUhEUgAAAIAAAACACAYAAADDPmHLAAAACXBIWXMAACxLAAAsSwGlPZapAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAOTSURBVHgB7d3tUdtAEMbxVSqgBKcD6MBUAFSQoYKQCmIqIKkgTgWmAycVQAdWB0kHl72RMqMwxIgX7+3d/n8zGs/AJ7QPp0U6ViIAAAAAAAAAAAAA2tRJISmlpX6c6XGux0Jiuh+P667reinAPABa+CP9WOnxUTD1RYYg/BZDpgEYi7/V41jwmLwanFqG4J3YWgnF3yefm89iyGwF0N/+hX7sBHPkVeCHGLBcAUyTXbkzMWIZAJb++c7FiOUlIAlm00uASW2sm0A4QwCCIwDBEYDgCEBwBCA4AhAcAQiOAARHAIIjAMERgOAIQHAEIDgCEBwBCI4ABEcAgiMAwRGA4AhAcAQgOAIQHAEIjgAERwCCIwDBEYDgCEBwBCA4AhAcAQjOMgCm488q1+SUsHvBXGbnyjIA3wVzrcWI9aDIO2FY1FP6ruveixHrJvBC6AX2yefmVAyZBmAciPxJ8D8r66HR5n8G6g+4lmFkLP6Vi/9VjJUcF78Rw4GIzt1q8S+kgJIByJPDc1O4kNh6GWYD91JAsQBk4wDpHIIjiSk3fSelip8VvRU8/uBFlj4nLksWPyv+LGAci76SeHLTdyuFFb0ETOnlYK0fHySGtRb/UhzwFIAor5PpZbjuu7gh5uZx8HhCcj/QS7t6MX4n0FPcrAB/ja+T20qbllr8n+KIuw0hY1PY4u3ilbfiu6YrwU1qx4045e4SMJXaeHx8r7/5J+KU9z2BtTeFvTi/0eV6Bch0FcgrQG4Ka7xdnP/cc70Vzv2u4PEEXkt9rrwXvyqprqawmpdkur8ETOmJzZeCpfjmuul7qLZ/DPHeFPZS2dPNqlaALPndQ1D82f5LVPevYY43lq5qK37VcqOV/ODN6CXoid+k8jZSsep6gKlUfmNpL46e7b9E1QHIUrmmsMqm76Hq5wMU3FhafEMnJpJtU0jT55EWZp0O75vAJy3OkR536XB2aWg84ZUWaDEW6hDFXwj800It09tjsEVNtGBX6e3Q9NUovc0eArcbOjFDel1TeCeoW3p5U7hLNH1t0EIe6/ErPQ9NX0vS85pCmr4WaWG3M4q/E7Qpzbs/EGVGQTxpuFX8lFC3eqvfD/BcucL7vq+PeEOdE94XEBwBCI4ABEcAgiMAwRGA4AhAcAQgOAIQXMQA8M6iiYgB2De3p/j0bhxYGnYIPbY5JH9tIWjfGILNpPBbig8AAAAAAAAAAACgLX8AbK0w0eyq6CIAAAAASUVORK5CYII=";
		private static Sprite playheadIcon;

		public static Sprite LoadPlayheadIcon()
		{
			if (playheadIcon == null)
			{
				Texture2D texture = new Texture2D(3, 3);
				texture.LoadImage(Convert.FromBase64String(playheadIcon_base64));

				playheadIcon = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
			}
			return playheadIcon;
		}

		public static float GetEventDurationWithFloorSpeed(LevelEvent evt)
		{
			scnEditor editor = scnEditor.instance;
			scrFloor floor = editor.floors[evt.floor];

			object f;
			bool valueExist = evt.data.TryGetValue("duration", out f);
			float duration = valueExist ? (float)f * (1 / floor.speed) : 0;

			return duration;
		}

		public static float TimeToBeat(double time)
		{
			return (float)(time / scrConductor.instance.crotchetAtStart);
		}

		public static float timelineWidth = 200f;
		public static float timelineHeight = 30f;
		public static float timelineScale = 1f;

		public static float GetEventPosX(LevelEvent evt)
		{
			scnEditor editor = scnEditor.instance;
			scrFloor floor = editor.floors[evt.floor];

			float position = TimeToBeat(floor.entryTime) * timelineWidth * timelineScale;

			object f;
			bool valueExist = evt.data.TryGetValue("angleOffset", out f);
			position += (valueExist ? (float)f : 0) / 180f * (1 / floor.speed) * timelineWidth;

			return position + timelineHeight;
		}

		public static float GetEventObjWidth(LevelEvent evt)
		{
			float duration = ETUtils.GetEventDurationWithFloorSpeed(evt);
			float objWidth = Mathf.Max(duration * timelineWidth, timelineHeight);

			return objWidth;
		}

		public static int GetRowNumber(LevelEvent evt)
		{
			if (evt.data.ContainsKey("row"))
			{
				string[] rowData = ((string)evt["row"]).Split(':');
				int rowNum;
				try
				{
					rowNum = Convert.ToInt32(rowData[1]);
				}
				catch
				{
					rowNum = -1;
				}
				return rowNum;
			}
			else
				return -1;
		}

		public static int GetRowGroup(LevelEvent evt)
		{
			if (evt.data.ContainsKey("row"))
			{
				string[] rowData = ((string)evt["row"]).Split(':');
				int rowGroup;
				try
				{
					rowGroup = Convert.ToInt32(rowData[0]);
				}
				catch
				{
					rowGroup = -1;
				}
				return rowGroup;
			}
			else
				return -1;
		}

		public static void SetRowNumber(LevelEvent evt, int rowNumber)
		{
			if (!evt.data.ContainsKey("row"))
				evt["row"] = $"-1:{rowNumber}";
			else
			{
				string[] rowData = ((string)evt["row"]).Split(':');
				evt["row"] = $"{rowData[0]}:{rowNumber}";
			}
		}

		public static void SetRowGroup(LevelEvent evt, int rowGroup)
		{
			if (!evt.data.ContainsKey("row"))
				evt["row"] = $"{rowGroup}:-1";
			else
			{
				string[] rowData = ((string)evt["row"]).Split(':');
				evt["row"] = $"{rowGroup}:{rowData[1]}";
			}
		}
	}
}
