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
						lightning = GameData.Atmos.Lightning.activeSelf
					};

					var pack = PacketManager.GetOrCreatePacket<WeatherPacket>(ClientConnectionManager.Instance.LocalPlayerID, PacketType.WEATHER_DATA);
					pack.weatherData = data;
					pack.targetPlayerIDs = SharedNPCSyncManager.Instance.GetPlayerSendList();
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
			

			//GameData.Atmos.ColorWeight = data.colorWeight;
			if (!isCheck)
			{
				GameData.Atmos.WeightGoal = data.weightGoal;
				//GameData.Atmos.isRaining = data.raining;

				lastData = data;
			}
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
		}
	}
}
