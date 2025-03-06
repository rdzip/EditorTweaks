using ADOFAI.Editor;
using ADOFAI.Editor.Preferences;
using ADOFAI.Editor.Preferences.Controls;
using EditorTweaks.Patch;
using EditorTweaks.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EditorTweaks.ADOFAI.EditorPreferencesControls
{
	public class EditorPreferencesKeybind : EditorPreferencesValueControl<List<EditorKeybind>>
	{
		public static EditorPreferencesTabButton ButtonTemplate;

		int currentIndex = -1;
		EditorPreferencesTabButton[] keybindButtons;
		EditorKeybind[] keybinds;

		public override EditorPreferencesControlType controlType => EditorPreferencesControlType.Vertical;

		public EditorPreferencesKeybind(Get getter, Set setter) : base(getter, setter)
		{
		}

		public override void Instantiate(RectTransform transform)
		{
			var layout = new GameObject("Layout");
			var layoutGroup = layout.AddComponent<HorizontalLayoutGroup>();
			layoutGroup.childAlignment = TextAnchor.UpperRight;
			layoutGroup.childForceExpandHeight = false;
			layoutGroup.childControlWidth = true;
			layoutGroup.childForceExpandWidth = true;
			layoutGroup.spacing = 5f;
			layoutGroup.padding = new RectOffset(0, 0, 0, 0);
			layout.AddComponent<LayoutElement>().preferredHeight = 32;
			layout.GetComponent<LayoutElement>().flexibleWidth = 1;
			layout.transform.SetParent(transform);

			keybindButtons = new EditorPreferencesTabButton[2];

			keybindButtons[0] = UnityEngine.Object
				.Instantiate(ButtonTemplate, layout.transform);

			keybindButtons[0].gameObject.SetActive(true);
			keybindButtons[0].text.alignment = TextAlignmentOptions.Center;
			keybindButtons[0].image.color = Color.white;
			keybindButtons[0].text.text = "";
			keybindButtons[0].text.overflowMode = TextOverflowModes.Truncate;
			keybindButtons[0].GetComponent<LayoutElement>().preferredHeight = 32;
			keybindButtons[0].GetComponent<LayoutElement>().flexibleWidth = 1;

			keybindButtons[1] = UnityEngine.Object
				.Instantiate(keybindButtons[0], layout.transform);

			keybindButtons[0].button.onClick.AddListener(delegate ()
			{
				if (!Input.GetMouseButtonUp(0)) return;
				KeybindPatch.CurrentEditingKeybind = this;
				keybindButtons[0].image.color = scnEditor.instance.editingColor;
				EventSystem.current.SetSelectedGameObject(null);
				currentIndex = 0;
			});

			keybindButtons[1].button.onClick.AddListener(delegate ()
			{
				if (!Input.GetMouseButtonUp(0)) return;
				KeybindPatch.CurrentEditingKeybind = this;
				keybindButtons[1].image.color = scnEditor.instance.editingColor;
				EventSystem.current.SetSelectedGameObject(null);
				currentIndex = 1;
			});

			keybinds = new EditorKeybind[2];
			keybinds[0] = new EditorKeybind(KeyModifier.None, KeyCode.None);
			keybinds[1] = new EditorKeybind(KeyModifier.None, KeyCode.None);

			if (this.Getter != null)
			{
				var kb = this.Getter();
				if (kb.Count > 0)
				{
					keybindButtons[0].text.text = KeybindToString(kb[0]);
					keybinds[0] = kb[0];
					if (kb.Count > 1)
					{
						keybindButtons[1].text.text = KeybindToString(kb[1]);
						keybinds[1] = kb[1];
					}
				}
			}
		}

		public string KeybindToString(EditorKeybind keybind)
		{
			if (keybind.key == KeyCode.None && keybind.modifierMask == KeyModifier.None)
				return "";
			return RDEditorUtils.KeyComboToString(
				keybind.modifierMask.HasFlag(KeyModifier.Control),
				keybind.modifierMask.HasFlag(KeyModifier.Shift),
				keybind.modifierMask.HasFlag(KeyModifier.Alt),
				keybind.key, keybind.ctrlIsCmd);
		}

		public void ChangingKeybind(EditorKeybind keybind)
		{
			if (currentIndex < 0) return;

			keybindButtons[currentIndex].text.text = KeybindToString(keybind);
		}

		public void FinishKeybind(EditorKeybind keybind)
		{
			ETLogger.Debug("FinishKeybind");
			keybindButtons[currentIndex].image.color = Color.white;
			keybindButtons[currentIndex].text.text = KeybindToString(keybind);
			keybinds[currentIndex] = keybind;
			UpdateKeybindManager();
			EventSystem.current.SetSelectedGameObject(keybindButtons[currentIndex].button.gameObject);
			currentIndex = -1;
		}

		public void CancelKeybind()
		{
			if (currentIndex < 0) return;
			keybindButtons[currentIndex].image.color = Color.white;
			keybindButtons[currentIndex].text.text = KeybindToString(keybinds[currentIndex]);
			currentIndex = -1;
		}

		public void UpdateKeybindManager()
		{
			if (this.Setter != null)
			{

				//TODO: Set Keybind
			}
		}
	}
}
