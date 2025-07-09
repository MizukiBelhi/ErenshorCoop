using System.Collections;
using ErenshorCoop.Shared;
using ErenshorCoop.Shared.Packets;
using UnityEngine;

namespace ErenshorCoop.Client
{
	public static class WeatherHandler
	{
		private static WeatherData lastData = new();
		private static Coroutine _cr;
		public static void Init()
		{
			_cr = ClientConnectionManager.Instance.StartCoroutine(UpdateWeather());
		}

		public static void Stop()
		{
			if(_cr != null && ClientConnectionManager.Instance != null)
				ClientConnectionManager.Instance.StopCoroutine(_cr);
		}

		static IEnumerator UpdateWeather()
		{
			while (ClientConnectionManager.Instance.IsRunning)
			{
				yield return new WaitForSeconds(5);

				if (ClientZoneOwnership.isZoneOwner)
				{
					var data = new WeatherData
					{
						hour = GameData.Time.hour,
						minute = GameData.Time.min,
						sinceRain = GameData.Atmos.SinceRain,
						cloudAmount = GameData.Atmos.CloudAmt,
						cloudiness = GameData.Atmos.Cloudiness,
						rainChance = GameData.Atmos.RainChance,
						colorWeight = GameData.Atmos.ColorWeight,
						weightGoal = GameData.Atmos.WeightGoal,
						raining = GameData.Atmos.isRaining,
						lightning = GameData.Atmos.Lightning.activeSelf,
						sunParentRotation = GameData.Time.SunParent.rotation,
						rotationZAmount = GameData.Time.rotAmt.z
					};

					var pack = PacketManager.GetOrCreatePacket<WeatherPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.WEATHER_DATA);
					pack.weatherData = data;
					pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
					count = 0;
				}
				else
				{
					if (GameData.Atmos.Rain.activeSelf != lastData.raining || GameData.Atmos.Lightning.activeSelf != lastData.lightning)
					{
						GameData.Atmos.isRaining = lastData.raining;
						ReceiveWeatherData(lastData, true);
					}
				}
			}
			
		}

		public static void ReceiveWeatherData(WeatherData data, bool isCheck=false)
		{
			if (!ClientConfig.WeatherSync.Value) return;

			GameData.Time.hour = data.hour;
			GameData.Time.min = data.minute;
			GameData.Atmos.SinceRain = data.sinceRain;
			GameData.Atmos.CloudAmt = data.cloudAmount;
			GameData.Atmos.Cloudiness = data.cloudiness;
			GameData.Atmos.RainChance = data.rainChance;
			
			GameData.Atmos.Rain.SetActive(data.raining);
			GameData.Atmos.Lightning.SetActive(data.lightning);

			if (GameData.Time.SunParent != null)
				GameData.Time.SunParent.rotation = data.sunParentRotation;

			GameData.Time.rotAmt.z = data.rotationZAmount;


			GameData.Atmos.ColorWeight = data.colorWeight;
			if (!isCheck)
			{
				GameData.Atmos.WeightGoal = data.weightGoal;
				//GameData.Atmos.isRaining = data.raining;

				lastData = data;
			}

			SetAtmosphere(data);
		}

		public static float count = 0;
		public static bool DayNightFixedUpd_Prefix()
		{
			if (!ClientConnectionManager.Instance.IsRunning || ClientZoneOwnership.isZoneOwner) return true;

			
			if (count < 60f)
			{
				count += 60f * Time.deltaTime * (float)GameData.Time.TimeScale;
				GameData.Time.SunParent.Rotate(GameData.Time.rotAmt.z * Vector3.forward * 60f * Time.deltaTime * (float)GameData.Time.TimeScale);
			}

			if (count >= 60f)
			{
				GameData.Time.min++;
				if (count > 60f)
				{
					count -= 60f;
				}
				else
				{
					count = 0f;
				}
			}
			if (GameData.Time.hour == 4 && GameData.Time.min > 45 && GameData.Time.rotAmt.z != -0.003f)
			{
				GameData.Time.SunParent.localEulerAngles = Vector3.Lerp(GameData.Time.SunParent.localEulerAngles, new Vector3(0f, GameData.Time.SunParent.localEulerAngles.y, 109.749f), 0.02f * (float)GameData.Time.TimeScale);
				GameData.Time.rotAmt.z = -0.003f;
			}
			

			return false;
		}

		private static float fogFarDist = 1200f;
		private static float fogNearDist = 100f;
		private static float fogDensity = 0.0005f;
		private static float fogFar;
		private static float fogNear;
		private static bool day = true;
		private static Light Sun;

		public static bool AtmosphereUpdate_Prefix(AtmosphereColors __instance)
		{
			if (!ClientConnectionManager.Instance.IsRunning || ClientZoneOwnership.isZoneOwner) return true;

			if (Sun == null)
				Sun = __instance.GetComponentInChildren<Light>();

			if (GameData.Time.GetHour() == 5 && __instance.CallColor != __instance.Dawn)
			{
				day = true;
				if (PlayerPrefs.GetInt("SHAD", 1) == 1)
					Sun.shadows = LightShadows.Soft;
				__instance.CallColor = __instance.Dawn;
				__instance.SunCallColor = __instance.SunDawn;
				__instance.SkyCallColor = __instance.SkyDawn;
				fogNearDist = 75f;
				fogFarDist = 800f;
				fogDensity = 0.008f;
			}
			if (GameData.Time.GetHour() == 6 && __instance.CallColor != __instance.Morning)
			{
				__instance.CallColor = __instance.Morning;
				__instance.SunCallColor = __instance.SunMorning;
				__instance.SkyCallColor = __instance.SkyMorning;
				fogNearDist = 75f;
				fogFarDist = 1000f;
				fogDensity = 0.003f;
			}
			if (GameData.Time.GetHour() >= 7 && GameData.Time.GetHour() < 17 && __instance.CallColor != __instance.Afternoon)
			{
				__instance.CallColor = __instance.Afternoon;
				__instance.SunCallColor = __instance.SunDay;
				__instance.SkyCallColor = __instance.SkyDay;
				fogNearDist = 75f;
				fogFarDist = 3500f;
				fogDensity = 0.002f;
			}
			if (GameData.Time.GetHour() == 17 && GameData.Time.GetHour() < 20 && __instance.CallColor != __instance.Evening)
			{
				__instance.CallColor = __instance.Evening;
				__instance.SunCallColor = __instance.SunEvening;
				__instance.SkyCallColor = __instance.SkyEvening;
				fogNearDist = 75f;
				fogFarDist = 1500f;
				fogDensity = 0.004f;
			}
			if (GameData.Time.GetHour() == 20 && GameData.Time.GetHour() < 22 && __instance.CallColor != __instance.Dusk)
			{
				__instance.CallColor = __instance.Dusk;
				__instance.SunCallColor = __instance.SunDusk;
				__instance.SkyCallColor = __instance.SkyDusk;
				fogNearDist = 75f;
				fogFarDist = 800f;
				fogDensity = 0.008f;
			}
			if (GameData.Time.GetHour() >= 22 && __instance.CallColor != __instance.Night)
			{
				day = false;
				Sun.shadows = LightShadows.None;
				__instance.CallColor = __instance.Night;
				__instance.SunCallColor = __instance.SunNight;
				__instance.SkyCallColor = __instance.SkyNight;
				fogNearDist = 75f;
				fogFarDist = 2800f;
				fogDensity = 0.003f;
			}

			__instance.LiveColor = new Color(__instance.RainColor.r * __instance.ColorWeight, __instance.RainColor.g * __instance.ColorWeight, __instance.RainColor.b * __instance.ColorWeight);
			__instance.LiveCloudColor = new Color(__instance.LiveColor.r * 1.3f, __instance.LiveColor.g * 1.3f, __instance.LiveColor.b * 1.4f);
			__instance.RainFog = __instance.ColorWeight * 500f;

			if (__instance.WeightGoal > 0.1f)
			{
				fogFar = fogFarDist / (__instance.WeightGoal * 15f);
				fogNear = fogNearDist / (__instance.WeightGoal * 30f);
				if (fogNear < 35f)
					fogNear = 35f;
				if (fogNear > fogFar)
					fogNear -= 60f * Time.deltaTime;
			}
			else
			{
				fogFar = fogFarDist;
			}
			if (__instance.isRaining)
			{
				fogNear = 25f;
				if ( fogFar > 800f)
					fogFar -= 60f * Time.deltaTime;
			}

			if (__instance.ColorWeight != __instance.WeightGoal)
				__instance.ColorWeight = Mathf.Lerp(__instance.ColorWeight, __instance.WeightGoal, Time.deltaTime);

			__instance.Cloudiness = Mathf.Lerp(__instance.Cloudiness, GameData.Atmos.Cloudiness, Time.deltaTime / 20f);

			if (__instance.Cloudiness < 0f)
				__instance.Cloudiness = 0f;
			if (__instance.Cloudiness > 0.8f)
				__instance.Cloudiness = 0.8f;
			__instance.SkyMat.SetFloat("_Cloudiness", __instance.Cloudiness);

			if (__instance.Cloudiness > 0.5f && !__instance.isRaining)
			{
				__instance.Cloudiness = Mathf.Lerp(__instance.Cloudiness, 0.25f, Time.deltaTime / 10f);
			}

			if (Sun.enabled)
			{
				if (GameData.Atmos.Rain.activeSelf)
				{
					if (!GameData.PlayerControl.Surfaced && GameData.PlayerControl.Swimming)
						__instance.Rain.SetActive(false);
				}
				else
				{
					if (__instance.isRaining && (GameData.PlayerControl.Surfaced || !GameData.PlayerControl.Swimming))
						__instance.Rain.SetActive(true);
				}
			}
			else
			{
				if (__instance.Rain.activeSelf)
					__instance.Rain.SetActive(false);
				if (__instance.Lightning.activeSelf)
					__instance.Lightning.SetActive(false);
			}

			if (Sun.enabled)
			{
				if (!GameData.PlayerControl.Swimming)
				{
					if (!__instance.isRaining)
					{
						if (day)
							__instance.FogColor = __instance.CloudColDay - __instance.LiveCloudColor;
						else
							__instance.FogColor = __instance.CloudColNight - __instance.LiveCloudColor;
					}
					else
					{
						__instance.FogColor = __instance.RainCloudCol;
					}
					if (Sun.enabled)
					{
						__instance.CurrentColor = Color.Lerp(__instance.CurrentColor, __instance.CallColor, 6E-05f * (float)GameData.Time.TimeScale);
						RenderSettings.ambientLight = __instance.CurrentColor;
						if (RenderSettings.fogEndDistance != fogFar)
						{
							RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, fogFar, 0.0006f * (float)GameData.Time.TimeScale);
						}
						if (RenderSettings.fogStartDistance != fogNear)
						{
							RenderSettings.fogStartDistance = Mathf.Lerp(RenderSettings.fogStartDistance, fogNear, 0.0009f * (float)GameData.Time.TimeScale);
						}
						if (RenderSettings.fogDensity != fogDensity)
						{
							RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, fogDensity, 6E-05f * (float)GameData.Time.TimeScale);
						}
					}
				}
				if (Sun.enabled)
				{
					__instance.CurrentSunColor = Color.Lerp(__instance.CurrentSunColor, __instance.SunCallColor, 6E-05f * (float)GameData.Time.TimeScale);
					Sun.color = __instance.CurrentSunColor;
				}
				__instance.SkyMat.SetColor("_CloudColorDay", __instance.CloudColDay - __instance.LiveCloudColor);
				__instance.SkyMat.SetColor("_CloudColorNight", __instance.SetNightCloud);
				__instance.SkyMat.SetColor("_GroundColor", RenderSettings.fogColor);
				__instance.CurSkyColor = Color.Lerp(__instance.CurSkyColor, __instance.SkyCallColor, 0.0006f * (float)GameData.Time.TimeScale);
				__instance.SkyMat.SetColor("_HorizonColorNight", __instance.CurSkyColor);
				__instance.SkyMat.SetColor("_HorizonColorDay", __instance.CurSkyColor);
				__instance.SkyMat.SetColor("_SkyColorNight", __instance.CurSkyColor);
				__instance.SkyMat.SetColor("_SkyColorDay", __instance.CurrentColor);
				__instance.SkyMat.SetFloat("_CloudOpacity", __instance.ColorWeight);
				__instance.GameSun.IntensityMod = Mathf.Lerp(__instance.GameSun.IntensityMod, __instance.Cloudiness, Time.deltaTime / 10f);
				if (!GameData.PlayerControl.Swimming)
					RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, __instance.FogColor, 0.06f * (float)GameData.Time.TimeScale);
				GameData.Wind.x = __instance.ColorWeight * 40f;
				GameData.Wind.y = __instance.ColorWeight * 20f;
				GameData.Wind.z = __instance.ColorWeight * 40f;
				return false;
			}
			if (GameData.Wind != Vector3.zero)
				GameData.Wind = Vector3.zero;

			return false;
		}


		public static void SetAtmosphere(WeatherData data)
		{
			GameData.Atmos.SinceRain = data.sinceRain;
			GameData.Atmos.CloudAmt = data.cloudAmount;
			GameData.Atmos.Cloudiness = data.cloudiness;
			GameData.Atmos.RainChance = data.rainChance;

			GameData.Atmos.Rain.SetActive(data.raining);
			GameData.Atmos.Lightning.SetActive(data.lightning);
			GameData.Atmos.WeightGoal = data.weightGoal;

			if (GameData.Time.GetHour() == 5)
			{
				day = true;
				if (PlayerPrefs.GetInt("SHAD", 1) == 1)
					Sun.shadows = LightShadows.Soft;
				GameData.Atmos.CallColor = GameData.Atmos.Dawn;
				GameData.Atmos.SunCallColor = GameData.Atmos.SunDawn;
				GameData.Atmos.SkyCallColor = GameData.Atmos.SkyDawn;
				fogNearDist = 75f;
				fogFarDist = 800f;
				fogDensity = 0.008f;
			}
			if (GameData.Time.GetHour() == 6)
			{
				GameData.Atmos.CallColor = GameData.Atmos.Morning;
				GameData.Atmos.SunCallColor = GameData.Atmos.SunMorning;
				GameData.Atmos.SkyCallColor = GameData.Atmos.SkyMorning;
				fogNearDist = 75f;
				fogFarDist = 1000f;
				fogDensity = 0.003f;
			}
			if (GameData.Time.GetHour() >= 7 && GameData.Time.GetHour() < 17)
			{
				GameData.Atmos.CallColor = GameData.Atmos.Afternoon;
				GameData.Atmos.SunCallColor = GameData.Atmos.SunDay;
				GameData.Atmos.SkyCallColor = GameData.Atmos.SkyDay;
				fogNearDist = 75f;
				fogFarDist = 3500f;
				fogDensity = 0.002f;
			}
			if (GameData.Time.GetHour() == 17 && GameData.Time.GetHour() < 20)
			{
				GameData.Atmos.CallColor = GameData.Atmos.Evening;
				GameData.Atmos.SunCallColor = GameData.Atmos.SunEvening;
				GameData.Atmos.SkyCallColor = GameData.Atmos.SkyEvening;
				fogNearDist = 75f;
				fogFarDist = 1500f;
				fogDensity = 0.004f;
			}
			if (GameData.Time.GetHour() == 20 && GameData.Time.GetHour() < 22)
			{
				GameData.Atmos.CallColor = GameData.Atmos.Dusk;
				GameData.Atmos.SunCallColor = GameData.Atmos.SunDusk;
				GameData.Atmos.SkyCallColor = GameData.Atmos.SkyDusk;
				fogNearDist = 75f;
				fogFarDist = 800f;
				fogDensity = 0.008f;
			}
			if (GameData.Time.GetHour() >= 22)
			{
				day = false;
				Sun.shadows = LightShadows.None;
				GameData.Atmos.CallColor = GameData.Atmos.Night;
				GameData.Atmos.SunCallColor = GameData.Atmos.SunNight;
				GameData.Atmos.SkyCallColor = GameData.Atmos.SkyNight;
				fogNearDist = 75f;
				fogFarDist = 2800f;
				fogDensity = 0.003f;
			}

			GameData.Atmos.LiveColor = new Color(GameData.Atmos.RainColor.r * data.colorWeight, GameData.Atmos.RainColor.g * data.colorWeight, GameData.Atmos.RainColor.b * data.colorWeight);
			GameData.Atmos.LiveCloudColor = new Color(GameData.Atmos.LiveColor.r * 1.3f, GameData.Atmos.LiveColor.g * 1.3f, GameData.Atmos.LiveColor.b * 1.4f);
			GameData.Atmos.RainFog = data.colorWeight * 500f;

			if (data.weightGoal > 0.1f)
			{
				fogFar = fogFarDist / (data.weightGoal * 15f);
				fogNear = fogNearDist / (data.weightGoal * 30f);
				if (fogNear < 35f)
					fogNear = 35f;
				if (fogNear > fogFar)
					fogNear = fogFar;
			}
			else
			{
				fogFar = fogFarDist;
			}
			if (data.raining)
			{
				fogNear = 25f;
				if (fogFar > 800f)
					fogFar = 800f;
			}

			GameData.Atmos.ColorWeight = data.weightGoal;

			GameData.Atmos.Cloudiness = data.cloudiness;
			GameData.Atmos.SkyMat.SetFloat("_Cloudiness", GameData.Atmos.Cloudiness);


			if (Sun.enabled)
			{
				if (GameData.Atmos.Rain.activeSelf)
				{
					if (!GameData.PlayerControl.Surfaced && GameData.PlayerControl.Swimming)
						GameData.Atmos.Rain.SetActive(false);
				}
				else
				{
					if (data.raining && (GameData.PlayerControl.Surfaced || !GameData.PlayerControl.Swimming))
						GameData.Atmos.Rain.SetActive(true);
				}
			}
			else
			{
				if (GameData.Atmos.Rain.activeSelf)
					GameData.Atmos.Rain.SetActive(false);
				if (GameData.Atmos.Lightning.activeSelf)
					GameData.Atmos.Lightning.SetActive(false);
			}

			if (Sun.enabled)
			{
				if (!GameData.PlayerControl.Swimming)
				{
					if (!data.raining)
					{
						if (day)
							GameData.Atmos.FogColor = GameData.Atmos.CloudColDay - GameData.Atmos.LiveCloudColor;
						else
							GameData.Atmos.FogColor = GameData.Atmos.CloudColNight - GameData.Atmos.LiveCloudColor;
					}
					else
					{
						GameData.Atmos.FogColor = GameData.Atmos.RainCloudCol;
					}
					if (Sun.enabled)
					{
						GameData.Atmos.CurrentColor = GameData.Atmos.CallColor;
						RenderSettings.ambientLight = GameData.Atmos.CurrentColor;
						if (RenderSettings.fogEndDistance != fogFar)
						{
							RenderSettings.fogEndDistance = fogFar;
						}
						if (RenderSettings.fogStartDistance != fogNear)
						{
							RenderSettings.fogStartDistance = fogNear;
						}
						if (RenderSettings.fogDensity != fogDensity)
						{
							RenderSettings.fogDensity = fogDensity;
						}
					}
				}
				if (Sun.enabled)
				{
					GameData.Atmos.CurrentSunColor = GameData.Atmos.SunCallColor;
					Sun.color = GameData.Atmos.CurrentSunColor;
				}
				GameData.Atmos.SkyMat.SetColor("_CloudColorDay", GameData.Atmos.CloudColDay - GameData.Atmos.LiveCloudColor);
				GameData.Atmos.SkyMat.SetColor("_CloudColorNight", GameData.Atmos.SetNightCloud);
				GameData.Atmos.SkyMat.SetColor("_GroundColor", RenderSettings.fogColor);
				GameData.Atmos.CurSkyColor = GameData.Atmos.SkyCallColor;
				GameData.Atmos.SkyMat.SetColor("_HorizonColorNight", GameData.Atmos.CurSkyColor);
				GameData.Atmos.SkyMat.SetColor("_HorizonColorDay", GameData.Atmos.CurSkyColor);
				GameData.Atmos.SkyMat.SetColor("_SkyColorNight", GameData.Atmos.CurSkyColor);
				GameData.Atmos.SkyMat.SetColor("_SkyColorDay", GameData.Atmos.CurrentColor);
				GameData.Atmos.SkyMat.SetFloat("_CloudOpacity", GameData.Atmos.ColorWeight);
				GameData.Atmos.GameSun.IntensityMod = GameData.Atmos.Cloudiness;
				if (!GameData.PlayerControl.Swimming)
					RenderSettings.fogColor = GameData.Atmos.FogColor;
				GameData.Wind.x = GameData.Atmos.ColorWeight * 40f;
				GameData.Wind.y = GameData.Atmos.ColorWeight * 20f;
				GameData.Wind.z = GameData.Atmos.ColorWeight * 40f;
				return;
			}
			if (GameData.Wind != Vector3.zero)
				GameData.Wind = Vector3.zero;
		}



		public struct WeatherData
		{
			public int hour;
			public int minute;
			public float sinceRain;
			public float cloudiness;
			public float rainChance;
			public float colorWeight;
			public float cloudAmount;
			public float weightGoal;
			public bool raining;
			public bool lightning;
			public Quaternion sunParentRotation;
			public float rotationZAmount;
		}
	}
}
