using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ErenshorCoop.UI
{
	public class TabManager
	{


		private struct butNfo
		{
			public string name;
			public Action callback;
			public GameObject cnt;
		}

		private List<butNfo> tabNfo = new();

		private struct butStr{
			public string name;
			public Button go;
			public TabButton but;
		}

		private List<butStr> tabButtons = new();
		public int currentActiveTab = -1;

		public GameObject tabButtonsContainer;


		public void AddTab(string tabName, GameObject tabContent, Action cb)
		{
			tabNfo.Add(new() { name = tabName, cnt = tabContent, callback = cb});
		}

		public void Cleanup()
		{
			if (tabButtonsContainer != null)
				UnityEngine.Object.Destroy(tabButtonsContainer);
		}

		public void HideTab(string name)
		{
			foreach(var tab in tabButtons)
			{
				if(tab.name == name)
				{
					tab.go.gameObject.SetActive(false);
				}
			}
		}

		public void ShowTab(string name)
		{
			foreach (var tab in tabButtons)
			{
				if (tab.name == name)
				{
					tab.go.gameObject.SetActive(true);
				}
			}
		}

		public GameObject CreateTabbedUI(Transform parent)
		{

			tabButtonsContainer = new GameObject("TabButtonsContainer");
			tabButtonsContainer.transform.SetParent(parent, false);
			RectTransform buttonsRect = tabButtonsContainer.AddComponent<RectTransform>();
			buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
			buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
			buttonsRect.pivot = new Vector2(0.5f, 0.5f);
			buttonsRect.sizeDelta = new Vector2(600f, 50);
			buttonsRect.anchoredPosition = new Vector2(0, 373);

			tabButtonsContainer.transform.SetAsFirstSibling();

			float xOffset = -250+5;

			for (int i = 0; i < tabNfo.Count; i++)
			{
				GameObject buttonGO = new GameObject($"Button_{tabNfo[i].name.Replace(" ", "")}");
				buttonGO.transform.SetParent(tabButtonsContainer.transform, false);
				RectTransform rect = buttonGO.AddComponent<RectTransform>();
				rect.sizeDelta = new Vector2(100f, 25f);
				Button button = buttonGO.AddComponent<Button>();
				Image buttonImage = buttonGO.AddComponent<Image>();
				//buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
				buttonImage.material = new Material(Base.materials["UI_OUTLINE CHAT"]);
				buttonImage.sprite = Base.bgSprite;

				var outline = buttonGO.AddComponent<Outline>();
				outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
				outline.effectDistance = new Vector2(1f, -1f);

				button.image = buttonImage;

				GameObject textGO = new GameObject("Text");
				textGO.transform.SetParent(buttonGO.transform, false);
				RectTransform textRect = textGO.AddComponent<RectTransform>();
				textRect.anchorMin = Vector2.zero;
				textRect.anchorMax = Vector2.one;
				textRect.sizeDelta = Vector2.zero;


				TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();

				buttonText.text = tabNfo[i].name;
				buttonText.color = Color.white;
				buttonText.alignment = TextAlignmentOptions.Center;
				buttonText.fontSize = 14;

				Vector2 buttonPos = new Vector2(xOffset, 3);
				TabButton buttonFX= buttonGO.AddComponent<TabButton>();
				buttonFX.SetOriginalPosition(buttonPos);

				xOffset += 100f;

				tabButtons.Add(new(){ but = buttonFX, go = button, name = tabNfo[i].name });
				int tabIndex = i;
				button.onClick.AddListener(() => SetActiveTab(tabIndex));
			}


			SetActiveTab(0);
			return tabButtonsContainer;
		}

		public void SetActiveTab(int tabIndex)
		{
			if (tabIndex == currentActiveTab) return;

			if (tabIndex < 0 || tabIndex >= tabNfo.Count)
			{
				Logging.Log($"Attempted to set invalid tab index: {tabIndex}");
				return;
			}

			for (int i = 0; i < tabNfo.Count; i++)
			{
				tabNfo[i].cnt.SetActive(false);
			}

			tabNfo[tabIndex].cnt.SetActive(true);
			currentActiveTab = tabIndex;
			tabNfo[tabIndex].callback?.Invoke();

			var colorB = new ColorBlock
			{
				normalColor = Color.white,
				selectedColor = Color.white,
				disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f),
				pressedColor = Color.white,
				highlightedColor = Color.white,
				fadeDuration = 0.1f,
				colorMultiplier = 1f
			};

			for (int i = 0; i < tabButtons.Count; i++)
			{
				tabButtons[i].go.colors = colorB;
				if (i == tabIndex)
				{
					tabButtons[i].but.SetSelectionState(true);
					tabButtons[i].go.interactable = false;
				}
				else
				{
					tabButtons[i].but.SetSelectionState(false);
					tabButtons[i].go.interactable = true;
				}
			}
		}
	}
}
