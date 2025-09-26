using ErenshorCoop.Client.Grouping;
using ErenshorCoop.Shared;
using LunarCatsStudio.Compass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ErenshorCoop.Client
{
	public static class CompassHandler
	{
		private static CompassBarProLinear compass;
		private static List<string> markers = new();
		private static Dictionary<string, GameObject> screenMarkers = new();

		public static void Init()
		{
			compass = (CompassBarProLinear)GameData.PlayerControl.transform.GetComponent<CompassManager>()._compass[0];
		}

		public static void AddMarker(Entity player)
		{
			if (compass == null || markers.Contains(player.entityName)) return;

			if (ClientConfig.DisplayCompassMarker.Value)
			{
				compass.AddMarker(player.entityName, player.transform, compass._defaultMarkerPrefab, 0, compass._defaultMarkerPosY);
			}
			markers.Add(player.entityName);
			if (ClientConfig.DisplayOffScreenMarker.Value)
			{
				var m = CreateScreenMarker(player, UI.Base.starIcon);
				if (m != null)
					screenMarkers.Add(player.entityName, m);
			}
		}

		public static void RemoveMarker(Entity player)
		{
			if (compass == null || !markers.Contains(player.entityName)) return;

			compass.RemoveMarker(player.entityName);
			markers.Remove(player.entityName);
			if(screenMarkers.ContainsKey(player.entityName))
				UnityEngine.Object.Destroy(screenMarkers[player.entityName]);
			screenMarkers.Remove(player.entityName);
		}

		public static void ClearMarkers()
		{
			if (compass == null) return;

			foreach (var m in markers)
				compass.RemoveMarker(m);
			foreach(var m in screenMarkers)
				UnityEngine.Object.Destroy(m.Value);
			markers.Clear();
			screenMarkers.Clear();
		}

		public static GameObject CreateScreenMarker(Entity target, Sprite icon)
		{
			var cam = GameData.CamControl.ActualCam;
			if (cam == null) return null;

			var canvasGO = new GameObject("MarkerCanvas");
			var canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 5000;
			UnityEngine.Object.DontDestroyOnLoad(canvasGO);

			var iconGO = new GameObject("MarkerIcon");
			iconGO.transform.SetParent(canvasGO.transform);

			var image = iconGO.AddComponent<UnityEngine.UI.Image>();
			image.sprite = icon;
			image.raycastTarget = false;
			image.color = ClientConfig.OffScreenMarkerColor.Value;

			var rect = image.GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2(32, 32);

			var textGO = new GameObject("MarkerLabel");
			textGO.transform.SetParent(iconGO.transform);

			var text = textGO.AddComponent<TMPro.TextMeshProUGUI>();
			text.text = target.entityName;
			text.fontSize = 14;
			text.alignment = TMPro.TextAlignmentOptions.Center;
			text.color = Color.white;
			text.raycastTarget = false;

			var textRect = text.GetComponent<RectTransform>();
			textRect.sizeDelta = new Vector2(100, 20);
			textRect.anchoredPosition = new Vector2(0, -20);

			var tracker = canvasGO.AddComponent<WorldToScreenMarker>();
			tracker.target = target.transform;
			tracker.cam = cam;
			tracker.uiIcon = rect;
			tracker.playerID = target.entityID;
			tracker.uiImg = image;

			return canvasGO;
		}
	}

	public class WorldToScreenMarker : MonoBehaviour
	{
		public Transform target;
		public Camera cam;
		public RectTransform uiIcon;
		public UnityEngine.UI.Image uiImg;
		public short playerID;

		public float screenEdgePadding = 30f;

		private static Color invisColor = new Color(0, 0, 0, 0);

		void Update()
		{
			if (target == null || cam == null || uiIcon == null) return;

			if (ClientConfig.MarkersOnlyGroup.Value && !ClientGroup.IsPlayerInGroup(playerID, false))
			{
				uiImg.color = invisColor;
				return;
			}

			uiImg.color = ClientConfig.OffScreenMarkerColor.Value;

			Vector3 targetPos = target.position;
			Vector3 screenPoint = cam.WorldToScreenPoint(targetPos);
			Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0f) / 2f;

			Vector3 toTarget = screenPoint - screenCenter;

			if (screenPoint.z < 0)
				toTarget = -toTarget;

			Vector3 dir = toTarget.normalized;

			float edgeX = Screen.width / 2f - screenEdgePadding;
			float edgeY = Screen.height / 2f - screenEdgePadding;
			float scale = Mathf.Min(
				edgeX / Mathf.Abs(dir.x),
				edgeY / Mathf.Abs(dir.y)
			);
			Vector3 edgePos = screenCenter + dir * scale;
			Vector3 viewportPos = cam.WorldToViewportPoint(targetPos);
			bool isVisible =
				viewportPos.z > 0 &&
				viewportPos.x >= 0 && viewportPos.x <= 1 &&
				viewportPos.y >= 0 && viewportPos.y <= 1;

			if (isVisible)
				uiIcon.gameObject.SetActive(false);
			else
			{
				uiIcon.gameObject.SetActive(true);
				uiIcon.position = edgePos;
			}
		}
	}
}
