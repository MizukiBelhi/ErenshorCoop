using System.Collections.Generic;
using ErenshorCoop.Client;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ErenshorCoop.Shared.Packets
{
	public class WeatherPacket : BasePacket
	{
		public WeatherPacket() : base(DeliveryMethod.ReliableOrdered) { }
		public WeatherHandler.WeatherData weatherData;

		public override void Write(NetDataWriter writer)
		{
			writer.Put((byte)PacketType.WEATHER_DATA);
			writer.Put(targetPlayerIDs.Count);
			foreach (var p in targetPlayerIDs)
				writer.Put(p);

			writer.Put(weatherData.hour);
			writer.Put(weatherData.minute);
			writer.Put(weatherData.sinceRain);
			writer.Put(weatherData.cloudAmount);
			writer.Put(weatherData.cloudiness);
			writer.Put(weatherData.rainChance);
			writer.Put(weatherData.colorWeight);
			writer.Put(weatherData.weightGoal);
			writer.Put(weatherData.raining);
			writer.Put(weatherData.lightning);
		}

		public override void Read(NetPacketReader reader)
		{

			int c = reader.GetInt();
			targetPlayerIDs = new();
			for (var i = 0; i < c; i++)
			{
				targetPlayerIDs.Add(reader.GetShort());
			}

			weatherData = new WeatherHandler.WeatherData
			{
				hour = reader.GetInt(),
				minute = reader.GetInt(),
				sinceRain = reader.GetFloat(),
				cloudAmount = reader.GetFloat(),
				cloudiness = reader.GetFloat(),
				rainChance = reader.GetFloat(),
				colorWeight = reader.GetFloat(),
				weightGoal = reader.GetFloat(),
				raining = reader.GetBool(),
				lightning = reader.GetBool()
			};
		}
	}
}
