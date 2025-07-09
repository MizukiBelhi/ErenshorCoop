using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ErenshorCoop.UI
{
	public class SettingsPanel : IPanel
	{
		public static float yPos;

		private static float width;
		private static float height;

		public static Transform parent;
		private static Canvas canvas;


		public GameObject CreatePanel(Transform _parent, float _width, float _height, Canvas _canvas = null)
		{
			canvas = _canvas;
			parent = _parent;
			yPos = LobbyPanel.yPos;
			width = _width;
			height = _height;


			//Base.AddLabel(parent, new Vector2(0, yPos - 20), new Vector2(100, 14), "Sync Weather");
			//Base.AddToggle(parent, new Vector2(100, yPos - 20), new Vector2(16, 16), (bool _) => { });

			var _l = Base.AddLabel(parent, new Vector2(0, yPos - 25), new Vector2(300, 20), "Client Settings");
			_l.fontSize = 20;
			yPos -= 25;
			var im = Base.AddImage(parent, new Vector2(50, yPos-20), new Vector2(220, 2), null);
			im.color = new Color(0, 0, 0, 0.08f);
			yPos -= 5;

			foreach (var entry in ((IEnumerable<KeyValuePair<ConfigDefinition, ConfigEntryBase>>)ErenshorCoopMod.config))
			{
				ConfigDefinition def = entry.Key;
				ConfigEntryBase baseEntry = entry.Value;
				var entryType = baseEntry.GetType();
				Toggle tog = null;
				InputField inp = null;

				//Hack: im lazy
				if (def.Section != "Client Settings") continue;


				if (entryType == typeof(ConfigEntry<bool>))
				{
					var l = Base.AddLabel(parent, new Vector2(50, yPos - 20), new Vector2(200, 14), def.Key.Replace("!",""));
					l.alignment = TMPro.TextAlignmentOptions.Left;
					tog = Base.AddToggle(parent, new Vector2(250, yPos - 20), new Vector2(16, 16), (bool _) => { baseEntry.BoxedValue = _; });
					tog.isOn = (bool)baseEntry.BoxedValue;
					yPos -= 20;
				}
				if(entryType == typeof(ConfigEntry<string>))
				{
					if (def.Key == "IP" || def.Key == "Port") continue;

					var l = Base.AddLabel(parent, new Vector2(50, yPos - 20), new Vector2(200, 14), def.Key.Replace("!", ""));
					l.alignment = TMPro.TextAlignmentOptions.Left;
					inp = Base.AddInputField(parent, new Vector2(250, yPos - 20), new Vector2(200, 16), "");
					inp.text = (string)baseEntry.BoxedValue;
					inp.onValueChanged.AddListener((string v) => { baseEntry.BoxedValue = v; });
					yPos -= 20;
				}
				if (entryType == typeof(ConfigEntry<float>))
				{
					var l = Base.AddLabel(parent, new Vector2(50, yPos - 20), new Vector2(200, 14), def.Key.Replace("!", ""));
					l.alignment = TMPro.TextAlignmentOptions.Left;
					inp = Base.AddInputField(parent, new Vector2(250, yPos - 20), new Vector2(200, 16), "");
					inp.text = $"{(float)baseEntry.BoxedValue}";
					inp.onValueChanged.AddListener((string v) => { var c = float.TryParse(v, out var f); if(c) baseEntry.BoxedValue = f; });
					yPos -= 20;
				}



				if (entryType.IsGenericType && entryType.GetGenericTypeDefinition() == typeof(ConfigEntry<>))
				{
					var eventInfo = entryType.GetEvent("SettingChanged");
					if (eventInfo != null)
					{
						EventHandler handler = (s, e) =>
						{
							if(entryType == typeof(ConfigEntry<bool>))
								tog.isOn = (bool)baseEntry.BoxedValue;
							if (entryType == typeof(ConfigEntry<string>))
								inp.text = (string)baseEntry.BoxedValue;
							if (entryType == typeof(ConfigEntry<float>))
								inp.text = $"{(float)baseEntry.BoxedValue}";
						};
						eventInfo.AddEventHandler(baseEntry, handler);
					}
				}
			}


			yPos -= 30f;
			_l = Base.AddLabel(parent, new Vector2(0, yPos - 20), new Vector2(300, 20), "Host Settings");
			_l.fontSize = 20;
			yPos -= 25;
			im = Base.AddImage(parent, new Vector2(50, yPos - 20), new Vector2(400, 2), null);
			im.color = new Color(0, 0, 0, 0.08f);
			yPos -= 5;

			foreach (var entry in ((IEnumerable<KeyValuePair<ConfigDefinition, ConfigEntryBase>>)ErenshorCoopMod.config))
			{
				ConfigDefinition def = entry.Key;
				ConfigEntryBase baseEntry = entry.Value;
				var entryType = baseEntry.GetType();
				Toggle tog = null;
				InputField inp = null;

				//Hack: im lazy
				if (def.Section != "Host Settings") continue;


				if (entryType == typeof(ConfigEntry<bool>))
				{
					var l = Base.AddLabel(parent, new Vector2(50, yPos - 20), new Vector2(200, 14), def.Key.Replace("!", ""));
					l.alignment = TMPro.TextAlignmentOptions.Left;
					tog = Base.AddToggle(parent, new Vector2(250, yPos - 20), new Vector2(16, 16), (bool _) => { baseEntry.BoxedValue = _; });
					tog.isOn = (bool)baseEntry.BoxedValue;
					yPos -= 20;
				}
				if (entryType == typeof(ConfigEntry<string>))
				{
					if (def.Key == "IP" || def.Key == "Port") continue;

					var l = Base.AddLabel(parent, new Vector2(50, yPos - 20), new Vector2(200, 14), def.Key.Replace("!", ""));
					l.alignment = TMPro.TextAlignmentOptions.Left;
					inp = Base.AddInputField(parent, new Vector2(250, yPos - 20), new Vector2(200, 16), "");
					inp.text = (string)baseEntry.BoxedValue;
					inp.onValueChanged.AddListener((string v) => { baseEntry.BoxedValue = v; });
					yPos -= 20;
				}
				if (entryType == typeof(ConfigEntry<float>))
				{
					var l = Base.AddLabel(parent, new Vector2(50, yPos - 20), new Vector2(200, 14), def.Key.Replace("!", ""));
					l.alignment = TMPro.TextAlignmentOptions.Left;
					inp = Base.AddInputField(parent, new Vector2(250, yPos - 20), new Vector2(200, 16), "");
					inp.text = $"{(float)baseEntry.BoxedValue}";
					inp.onValueChanged.AddListener((string v) => { var c = float.TryParse(v, out var f); if (c) baseEntry.BoxedValue = f; });
					yPos -= 20;
				}



				if (entryType.IsGenericType && entryType.GetGenericTypeDefinition() == typeof(ConfigEntry<>))
				{
					var eventInfo = entryType.GetEvent("SettingChanged");
					if (eventInfo != null)
					{
						EventHandler handler = (s, e) =>
						{
							if (entryType == typeof(ConfigEntry<bool>))
								tog.isOn = (bool)baseEntry.BoxedValue;
							if (entryType == typeof(ConfigEntry<string>))
								inp.text = (string)baseEntry.BoxedValue;
							if (entryType == typeof(ConfigEntry<float>))
								inp.text = $"{(float)baseEntry.BoxedValue}";

							ErenshorCoopMod.config.Save();
						};
						eventInfo.AddEventHandler(baseEntry, handler);
					}
				}
			}

			return null;
		}
	}
}
