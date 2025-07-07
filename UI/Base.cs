using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ErenshorCoop.UI
{
	public static class Base
	{
		public static Sprite roundedSprite;
		public static Dictionary<string, Material> materials = new();
		public static Dictionary<string, Sprite> sprites = new();

		public static void LoadSpritesAndMaterials()
		{
			var texture = new Texture2D(32, 32, TextureFormat.ARGB32, false);
			var pixels = new Color[32 * 32];
			for (var i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
			texture.SetPixels(pixels);
			texture.Apply();
			roundedSprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);


			List<string> matsToFind = new();
			matsToFind.Add("UI_OUTLINE CHAT");


			var allMaterials = Resources.FindObjectsOfTypeAll<Material>();

			foreach (string mat in matsToFind)
			{
				var _mat = allMaterials.FirstOrDefault(m => m.name == mat);

				if (_mat != null)
				{
					materials.Add(mat, _mat);
				}
				else
				{
					Logging.Log($"Could not find material {mat}");
				}
			}


			List<string> spritesToFind = new();
			spritesToFind.Add("cosmetics_and_essentials_carv 1");
			spritesToFind.Add("left_tab_h 1");
			spritesToFind.Add("heading_texture");

			foreach (string spr in spritesToFind)
			{
				var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();

				var _spr = allSprites.FirstOrDefault(s => s.name == spr);

				if (_spr != null)
				{
					sprites.Add(spr, _spr);
				}
				else
				{
					Logging.Log($"Could not find sprite {spr}");
				}
			}
		}

		public static TextMeshProUGUI AddLabel(Transform parent, Vector2 position, Vector2 size, string text)
		{
			GameObject go = new GameObject("Label", typeof(TextMeshProUGUI));
			go.transform.SetParent(parent, false);

			RectTransform rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = size;
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.pivot = new Vector2(0, 1);

			rt.anchoredPosition = position;

			TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
			txt.text = text;
			//txt.font = font;
			txt.fontSize = 14;
			txt.alignment = TextAlignmentOptions.Center;
			txt.color = Color.white;
			txt.enabled = true;
			go.SetActive(true);

			return txt;
		}

		public static InputField AddInputField(Transform parent, Vector2 position, Vector2 size, string placeholderText)
		{
			GameObject go = new GameObject(Random.Range(0,999).ToString()+"InputField", typeof(Image), typeof(InputField));
			go.transform.SetParent(parent, false);

			Image img = go.GetComponent<Image>();
			img.color = Color.white;
			img.sprite = sprites["left_tab_h 1"];


			RectTransform rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = size;
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.pivot = new Vector2(0, 1);
			rt.anchoredPosition = position;


			var placeholder = CreateText(go.transform, placeholderText, new Vector2(5, 0), size - new Vector2(5, 0), true);
			placeholder.raycastTarget = false;

			var inputText = CreateText(go.transform, "", new Vector2(5, 0), size - new Vector2(5, 0));
			inputText.raycastTarget = true;

			var input = go.GetComponent<InputField>();

			input.textComponent = inputText;
			input.placeholder = placeholder;
			//input.caretBlinkRate = 0.85f;
			//input.caretWidth = 1;
			input.targetGraphic = img;

			input.caretColor = Color.white;
			input.customCaretColor = true;
			//input.text = "fasfa";
			input.characterLimit = 20;

			input.interactable = true;
			input.enabled = true;
			input.shouldHideMobileInput = true;

			//var outline = go.AddComponent<Outline>();
			//outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
			//outline.effectDistance = new Vector2(1f, -1f);

			go.SetActive(true);
			return input;
		}

		public static Text CreateText(Transform parent, string text, Vector2 offset, Vector2 size, bool isPlaceholder = false)
		{
			GameObject go = new GameObject(isPlaceholder ? "Placeholder" : "Text", typeof(Text));
			go.transform.SetParent(parent, false);

			RectTransform rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = size;
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.pivot = new Vector2(0, 1);
			rt.anchoredPosition = offset;

			//go.transform.localPosition = position;

			Text txt = go.GetComponent<Text>();
			txt.text = text;
			txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			txt.fontSize = 14;
			txt.alignment = TextAnchor.MiddleLeft;
			txt.color = isPlaceholder ? Color.gray : Color.white;
			go.SetActive(true);

			return txt;
		}

		public static void AddImage(Transform parent, Vector2 position, Vector2 size, Sprite sprite)
		{
			GameObject go = new GameObject("Logo", typeof(RectTransform));
			go.transform.SetParent(parent, false);

			RectTransform rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = size;
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.pivot = new Vector2(0, 1);
			rt.anchoredPosition = position;

			Image img = go.AddComponent<Image>();
			img.sprite = sprite;
			go.SetActive(true);
		}

		public static Button AddButton(Transform parent, Vector2 position, Vector2 size, string text, UnityEngine.Events.UnityAction onClick)
		{
			GameObject go = new GameObject("Button", typeof(RectTransform));
			go.transform.SetParent(parent, false);

			RectTransform rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = size;
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.pivot = new Vector2(0, 1);
			rt.anchoredPosition = position;

			Image img = go.AddComponent<Image>();
			img.color = Color.white;
			img.sprite = sprites["heading_texture"];

			Button button = go.AddComponent<Button>();

			//var outline = go.AddComponent<Outline>();
			//outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
			//outline.effectDistance = new Vector2(1f, -1f);


			Text btnText = CreateText(go.transform, text, Vector2.zero, size);
			btnText.alignment = TextAnchor.MiddleCenter;

			button.onClick.AddListener(onClick);
			go.SetActive(true);
			return button;
		}
	}
}
