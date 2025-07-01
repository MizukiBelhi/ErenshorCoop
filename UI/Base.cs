using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
		public static Dictionary<string, TMP_FontAsset> tmpFonts = new();

		public static Sprite infoIcon;
		public static Sprite lockClosedIcon;
		public static Sprite lockOpenIcon;
		public static Sprite refreshIcon;
		public static Sprite filterIcon;
		public static Sprite checkmarkIcon;
		public static Sprite pingIcon;

		public static Sprite bgSprite;

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
			matsToFind.Add("UI_OUTLINE TRIM");

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
			spritesToFind.Add("dd_form 1");

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



			List<string> fontsToFind = new();
			fontsToFind.Add("Brokenz-BWa0d SDF");


			foreach (string fnt in fontsToFind)
			{
				var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

				var _fnt = allFonts.FirstOrDefault(s => s.name == fnt);

				if (_fnt != null)
				{
					tmpFonts.Add(fnt, _fnt);
				}
				else
				{
					Logging.Log($"Could not find font {fnt}");
				}
			}


			lockOpenIcon = LoadSpriteFromResource("ErenshorCoop.UI.icons.lock-open-solid.png");
			lockClosedIcon = LoadSpriteFromResource("ErenshorCoop.UI.icons.lock-solid.png");
			infoIcon = LoadSpriteFromResource("ErenshorCoop.UI.icons.circle-info-solid.png");
			refreshIcon = LoadSpriteFromResource("ErenshorCoop.UI.icons.rotate-solid.png");
			filterIcon = LoadSpriteFromResource("ErenshorCoop.UI.icons.list-solid.png");
			checkmarkIcon = LoadSpriteFromResource("ErenshorCoop.UI.icons.check-solid.png");
			pingIcon = LoadSpriteFromResource("ErenshorCoop.UI.icons.signal-solid.png");
			bgSprite = LoadSpriteFromResource("ErenshorCoop.UI.icons.bg.png", new Vector4(2,2,2,2));
		}


		public static Sprite LoadSpriteFromResource(string resourceName)
		{
			var assembly = Assembly.GetExecutingAssembly();

			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			{
				if (stream == null)
				{
					Debug.LogError($"[ResourceLoader] Resource not found: {resourceName}");
					return null;
				}

				byte[] data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);

				Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				tex.LoadImage(data, false);

				return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
			}
		}

		public static Sprite LoadSpriteFromResource(string resourceName, Vector4 rect)
		{
			var assembly = Assembly.GetExecutingAssembly();

			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			{
				if (stream == null)
				{
					Debug.LogError($"[ResourceLoader] Resource not found: {resourceName}");
					return null;
				}

				byte[] data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);

				Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				tex.LoadImage(data, false);

				var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, rect);

				return spr;
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

		public static TextMeshProUGUI AddLabel(Transform parent, Vector2 position, Vector2 size, string text, float fontSize)
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
			txt.fontSize = fontSize;
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

		public static Image AddImage(Transform parent, Vector2 position, Vector2 size, Sprite sprite)
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
			return img;
		}

		public static Button AddButton(Transform parent, Vector2 position, Vector2 size, string text, UnityEngine.Events.UnityAction onClick, Sprite sprite=null, bool overrideSpr = false)
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
			img.sprite = sprite!=null?sprite:sprites["heading_texture"];
			if (overrideSpr)
				img.sprite = sprite;
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

		public static Button AddButton(Transform parent, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax , Sprite sprite,string text, UnityEngine.Events.UnityAction onClick)
		{
			GameObject go = new GameObject("Button", typeof(RectTransform));
			go.transform.SetParent(parent, false);

			RectTransform rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = size;
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.pivot = new Vector2(0.5f, 0.5f);
			rt.anchoredPosition = position;

			Image img = go.AddComponent<Image>();
			img.color = Color.white;
			img.sprite = sprite;
			img.material = materials["UI_OUTLINE TRIM"];

			Button button = go.AddComponent<Button>();

			var colorB = new ColorBlock
			{
				normalColor = Color.white,
				selectedColor = new Color(0.9608f, 0.9608f, 0.9608f, 1f),
				disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f),
				pressedColor = new Color(0.8833f, 1f, 0f, 1f),
				highlightedColor = new Color(0.1114f, 1f, 0f, 1f),
				fadeDuration = 0.1f,
				colorMultiplier = 1f
			};

			button.colors = colorB;

			//var outline = go.AddComponent<Outline>();
			//outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
			//outline.effectDistance = new Vector2(1f, -1f);


			var textGO = new GameObject("Text", typeof(TextMeshProUGUI));
			textGO.transform.SetParent(go.transform, false);
			var tmp = textGO.GetComponent<TextMeshProUGUI>();
			tmp.text = text;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.font = tmpFonts["Brokenz-BWa0d SDF"];
			tmp.fontSize = 30;
			tmp.raycastTarget = false;
			var tmpRT = tmp.GetComponent<RectTransform>();
			tmpRT.anchorMin = Vector2.zero;
			tmpRT.anchorMax = Vector2.one;
			tmpRT.offsetMin = Vector2.zero;
			tmpRT.offsetMax = Vector2.zero;


			var col = go.AddComponent<ButtonColorText>();

			button.onClick.AddListener(onClick);
			go.SetActive(true);
			return button;
		}
	}
}
