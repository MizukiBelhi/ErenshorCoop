using System;
using System.Net.Http;
using UnityEngine;
using System.Collections;
using ErenshorCoop.Client;
using UnityEngine.Networking;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Linq;

namespace ErenshorCoop
{
	internal static class VersionCheck
	{
		static readonly string requestURI = "https://thunderstore.io/api/experimental/package/mizuki/Erenshor_COOP/";
		static bool done = false;

		enum PointType
		{
			STR,
			INT,
			DEX,
			END,
			WIS,
			CHA,
			AGI,
		}

		public static void StartRequest()
		{
			if(!done)
				ClientConnectionManager.Instance.StartCoroutine(Request());

			done = true;
		}

		private static IEnumerator Request()
		{
			var result = "";
			using (UnityWebRequest req = UnityWebRequest.Get(requestURI))
			{

				yield return req.SendWebRequest();
				if (req.result != UnityWebRequest.Result.Success)
				{
					result = "Error";
				}
				else
				{
					result = req.downloadHandler.text;
				}
			}

			if (string.IsNullOrEmpty(result) || result == "Error")
			{
				Logging.LogGameMessage("Could not receive version data.", true);
				yield break;
			}


			//Json doesnt want to work with it, so we're doing it manually
			//PackageData data = JsonUtility.FromJson<PackageData>(result);

			int start = result.IndexOf("\"version_number\":\"") + "\"version_number\":\"".Length;
			int end = result.IndexOf("\"", start);
			string versionNumber = result.Substring(start, end - start);

			var ver = new Version(versionNumber);
			var curVer = ErenshorCoopMod.version;
			var dif = curVer.CompareTo(ver);
			if (dif == 0)
			{
				Logging.LogGameMessage("Your version is up to date!");
			}
			else if (dif < 0)
			{
				Logging.LogGameMessage($"Version {ver} is now available! (Yours: v{curVer})", true);
			}
			else
			{
				Logging.LogGameMessage("You seem to be using a newer version!");
			}
			yield break;
		}
	}
}


