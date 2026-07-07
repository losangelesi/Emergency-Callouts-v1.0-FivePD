using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;

namespace SilverAlert
{
	public class SilverAlertLocation
	{
		private readonly float MIN_DISTANCE = 100f;
		private readonly float MAX_DISTANCE = 1500f;
		private readonly Vector3[] rpLocation = new Vector3[]
		{
			 new Vector3(-828.5627f, 167.4761f, 69.79989f),		//1
			 new Vector3(-837.5956f, 115.3527f, 55.35818f),		//2
			 new Vector3(-1561.07f, -407.53f, 42.38398f),		//3
			 new Vector3(-197.8682f, 139.1026f, 70.03341f),		//4
			 new Vector3(-661.835f, 477.3621f, 109.9556f),		//5
			 new Vector3(-1981.816f, 601.0262f, 118.3695f),		//6
			 new Vector3(1098.713f, -465.5878f, 67.31941f),		//7
			 new Vector3(-1330.399f, -934.1412f, 11.35233f),	//8
			 new Vector3(-1086.755f, -1502.282f, 4.974637f),	//9
			 new Vector3(-550.936f, -801.2344f, 30.69828f),		//10
			 new Vector3(1212.227f, -1608.437f, 50.34826f),		//11
			 new Vector3(285.3134f, -1983.242f, 21.20544f),		//12
			 new Vector3(334.4809f, -2057.07f, 20.93641f),		//13
			 new Vector3(131.6236f, -1895.276f, 23.38247f),		//14
			 new Vector3(-46.3531f, -1446.123f, 32.4296f),		//15
			 new Vector3(-173.9245f, -1547.104f, 35.12737f),	//16
			 new Vector3(-288.1686f, -819.9954f, 31.55603f),	//17
			 new Vector3(-920.7807f, 812.3511f, 184.3361f)		//18
		};
		private readonly float[] rpHeading = new float[]
		{
			134.6896f,		//1
			63.35522f,		//2
			229.9981f,		//3
			168.6183f,		//4
			349.08f,		//5
			188.0034f,		//6
			108.4845f,		//7
			329.8891f,		//8
			24.18739f,		//9
			183.9001f,		//10
			222.049f,		//11
			229.018f,		//12
			313.1955f,		//13
			322.2348f,		//14
			146.1411f,		//15
			53.70313f,		//16
			232.5828f,		//17
			177.657f		//18
		};
		private readonly Vector3[] dropoffLocation = new Vector3[]
		{
			new Vector3(-832.1899f, 167.8567f, 69.48675f),		//1
			new Vector3(-849.8403f, 117.0901f, 55.65044f),		//2
			new Vector3(-1551.044f, -402.7021f, 41.98772f),		//3
			new Vector3(-192.6591f, 134.333f, 69.67476f),		//4
			new Vector3(-657.5261f, 490.3018f, 109.7638f),		//5
			new Vector3(-1979.336f, 585.0654f, 117.395f),		//6
			new Vector3(1085.094f, -470.5249f, 64.56502f),		//7
			new Vector3(-1318.702f, -923.9981f, 11.20212f),		//8
			new Vector3(-1084.013f, -1497.958f, 4.636658f),		//9
			new Vector3(-550.806f, -826.6163f, 28.0945f),		//10
			new Vector3(1218.256f, -1618.86f, 48.96135f),		//11
			new Vector3(294.0698f, -1993.139f, 20.71896f),		//12
			new Vector3(331.8062f, -2044.967f, 20.79479f),		//13
			new Vector3(136.3273f, -1883.244f, 23.46637f),		//14
			new Vector3(-46.85745f, -1459.527f, 31.76178f),		//15
			new Vector3(-148.5895f, -1551.513f, 34.55268f),		//16
			new Vector3(-279.6424f, -821.5801f, 31.60237f),		//17
			new Vector3(-919.0477f, 801.6101f, 184.1676f)		//18
		};

		private int locationIndex = -1;

		public SilverAlertLocation()
		{
			List<int> eligibleLocations = new List<int>();
			float distance;
			Random rnd = new Random();
			//Select a random location within distance parameters
			for (int i = 0; i < rpLocation.Length; i++)
			{
				distance = World.GetDistance(rpLocation[i], Game.PlayerPed.Position);
				if (distance >= MIN_DISTANCE && distance < MAX_DISTANCE)
				{
					eligibleLocations.Add(i);
				}
			}
			if (eligibleLocations.Count < 1)
			{

				locationIndex = rnd.Next(0, rpLocation.Length);
			}
			else
			{
				locationIndex = eligibleLocations.ElementAt(rnd.Next(0, eligibleLocations.Count));
			}
		}

		public Vector3 getDropOffLocation()
		{
			return dropoffLocation[locationIndex];
		}
		public Vector3 getRPLocation()
		{
			return rpLocation[locationIndex];
		}
		public float getRPHeading()
		{
			return rpHeading[locationIndex];
		}
	}
}
