using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;

namespace ShopLifting 
{
	public class ShopliftingLocation
	{
		private readonly float MIN_DISTANCE = 100f;
		private readonly float MAX_DISTANCE = 1500f;
		private readonly string[] locationName = new string[]
		{
		"LTD",						//1
		"LTD",						//2
		"24/7",						//3
		"24/7",						//4
		"Robs",						//5
		"Robs",						//6
		"Robs",						//7
		"Ammunation",				//8
		"Ammunation",				//9
		"Ammunation",				//10
		"Ammunation",				//11
		"Ammunation",				//12
		"Ammunation",				//13
		"DiscountClothes",			//14
		"Bincos",					//15
		"Bincos",					//16
		"HatShop",					//17
		"BeachShop",				//18
		"Ponsonbys",				//19
		"SubUrban",					//20
		"SubUrban"                  //21
		};
		private readonly string[] locationComment = new string[]
		{
			"At the LTD Convenience Store. RP with suspect.",		//1
			"At the LTD Convenience Store. RP with suspect.",		//2
			"At the 24/7. RP with suspect.",						//3
			"At the 24/7. RP with suspect.",						//4
			"At the Rob's Liquor. RP with suspect.",				//5
			"At the Rob's Liquor. RP with suspect.",				//6
			"At the Rob's Liquor. RP with suspect.",				//7
			"At the AmmuNation. RP with suspect.",					//8
			"At the AmmuNation. RP with suspect.",					//9
			"At the AmmuNation. RP with suspect.",					//10
			"At the AmmuNation. RP with suspect.",					//11
			"At the AmmuNation. RP with suspect.",					//12
			"At the AmmuNation. RP with suspect.",					//13
			"At the Discout Clothing Store. RP with Suspect.",		//14
			"At the Binco's. RP with Suspect.",						//15
			"At the Binco's. RP with Suspect.",						//16
			"At the beachfront Hat Shop. RP with Suspect.",			//17
			"At a beachfront shop. RP with Suspect.",				//18
			"At the Ponsonby's. RP with Suspect.",					//19
			"At the Sub Urban. RP with Suspect.",					//20
			"At the Sub Urban. RP with Suspect."					//21
		};
		private readonly Vector3[] suspectLocation = new Vector3[]
		{
			new Vector3(-48.25007f, -1757.346f, 29.42102f),		//1
			new Vector3(1164.009f, -317.831f, 69.20506f),		//2
			new Vector3(29.71389f, -1340.811f, 29.49702f),		//3
			new Vector3(377.184f, 333.4744f, 103.5664f),		//4
			new Vector3(1136.132f, -981.2024f, 46.41585f),		//5
			new Vector3(-1487.327f, -379.1797f, 40.16343f),		//6
			new Vector3(-1226.141f, -908.1517f, 12.32635f),		//7
			new Vector3(824.585f, -2154.456f, 29.61901f),		//8
			new Vector3(253.4064f, -47.86467f, 69.94106f),		//9
			new Vector3(5.120412f, -1104.019f, 29.79703f),		//10
			new Vector3(846.2552f, -1033.455f, 28.19486f),		//11
			new Vector3(-1310.719f, -395.3238f, 36.69578f),		//12
			new Vector3(-666.0787f, -936.0092f, 21.82923f),		//13
			new Vector3(72.34982f, -1399.927f, 29.37615f),		//14
			new Vector3(-817.1957f, -1075.719f, 11.32811f),		//15
			new Vector3(429.213f, -808.2698f, 29.49114f),		//16
			new Vector3(-1336.437f, -1276.799f, 4.88463f),		//17
			new Vector3(-1340.261f, -1264.845f, 4.895197f),		//18
			new Vector3(-168.5653f, -299.9563f, 39.73328f),		//19
			new Vector3(122.6685f, -228.6163f, 54.55783f),		//20
			new Vector3(-1190.227f, -765.6083f, 17.31835f)		//21
		};
		private readonly float[] suspectHeading = new float[]
		{
			62.48516f,	//1
			163.5542f,	//2
			358.5281f,	//3
			114.0145f,	//4
			256.5131f,	//5
			188.8498f,	//6
			301.143f,	//7
			145.4808f,	//8
			88.28011f,	//9
			260.1234f,	//10
			70.08672f,	//11
			44.21824f,	//12
			248.5899f,	//13
			281.784f,	//14
			167.8851f,	//15
			179.6025f,	//16
			103.4237f,	//17
			125.449f,	//18
			309.752f,	//19
			316.8182f,	//20
			187.0073f	//21
		};
		private readonly Vector3[] rpLocation = new Vector3[]
		{
			new Vector3(-47.69429f, -1755.916f, 29.42102f),			//1
			new Vector3(1165.003f, -322.1297f, 69.20506f),			//2
			new Vector3(24.8275f, -1339.425f, 29.49702f),			//3
			new Vector3(373.1827f, 329.704f, 103.5664f),			//4
			new Vector3(1135.38f, -978.9224f, 46.41585f),			//5
			new Vector3(-1484.838f, -379.1033f, 40.16343f),			//6
			new Vector3(-1221.534f, -908.1858f, 12.32636f),			//7
			new Vector3(823.6397f, -2158.058f, 29.61902f),			//8
			new Vector3(252.6322f, -49.83448f, 69.94106f),			//9
			new Vector3(7.756627f, -1105.51f, 29.79703f),			//10
			new Vector3(841.9773f, -1033.509f, 28.19487f),			//11
			new Vector3(-1308.279f, -394.1167f, 36.69578f),			//12
			new Vector3(-661.9199f, -935.6894f, 21.82922f),			//13
			new Vector3(74.05354f, -1395.929f, 29.37615f),			//14
			new Vector3(-819.9882f, -1074.693f, 11.32811f),			//15
			new Vector3(426.8493f, -808.1058f, 29.49115f),			//16
			new Vector3(-1338.162f, -1276.065f, 4.895196f),			//17
			new Vector3(-1342.294f, -1263.975f, 4.895197f),			//18
			new Vector3(-163.6128f, -297.4836f, 39.7333f),			//19
			new Vector3(125.6642f, -227.1591f, 54.55783f),			//20
			new Vector3(-1194.575f, -767.4404f, 17.31619f),			//21
		};
		private readonly float[] rpHeading = new float[]
		{
			82.22276f,			//1
			116.2468f,			//2
			206.2702f,			//3
			264.9513f,			//4
			225.6105f,			//5
			114.0246f,			//6
			37.95729f,			//7
			54.86237f,			//8
			19.04055f,			//9
			307.8905f,			//10
			340.7051f,			//11
			69.62965f,			//12
			148.1132f,			//13
			276.184f,			//14
			202.8066f,			//15
			124.0834f,			//16
			157.7132f,			//17
			197.2899f,			//18
			176.7031f,			//19
			68.18866f,			//20
			195.4984f			//21
		};
		private int locationIndex = -1;

		public ShopliftingLocation()
		{
			List<int> eligibleLocations = new List<int>();
			float distance;
			Random rnd = new Random();
			//Select a random location within distance parameters
			for ( int i = 0; i < suspectLocation.Length; i++ )
            {
				distance = World.GetDistance(suspectLocation[i], Game.PlayerPed.Position);
				if( distance >= MIN_DISTANCE && distance < MAX_DISTANCE )
                {
					eligibleLocations.Add(i);
                }
			}
			if( eligibleLocations.Count < 1 )
            {
				
				locationIndex = rnd.Next(0, suspectLocation.Length);
			}
            else
            {
				locationIndex = eligibleLocations.ElementAt(rnd.Next(0, eligibleLocations.Count));
            }
		}

		public Vector3 getSuspectLocation()
        {
			return suspectLocation[locationIndex];
		}
		public Vector3 getRPLocation()
		{
			return rpLocation[locationIndex];
		}
		public float getSuspectHeading()
		{
			return suspectHeading[locationIndex];
		}
		public float getRPHeading()
		{
			return rpHeading[locationIndex];
		}
		public string getLocationName()
		{
			return locationName[locationIndex];
		}
		public string getLocationComment()
		{
			return locationComment[locationIndex];
		}
	}
}
