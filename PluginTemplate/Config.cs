using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using System.IO;
using Newtonsoft.Json;

namespace AverageTerrariaMain
{
    public class Config
    {
		public int[] DefaultChestIDs = new[]
		{
				168,
				20,
				22,
				40,
				42,
				28,
				292,
				298,
				299,
				290,
				8,
				31,
				72,
				280,
				284,
				281,
				282,
				279,
				285,
				21,
				289,
				303,
				291,
				304,
				49,
				50,
				52,
				53,
				54,
				55,
				51,
				43,
				167,
				188,
				295,
				302,
				305,
				73,
				301,
				159,
				65,
				158,
				117,
				265,
				294,
				288,
				297,
				300,
				218,
				112,
				220,
				985,
				267,
				156
			};

		public string infoMessage { get; set; } = "Insert text here!";

		public string discordMessage { get; set; } = "Insert discord here!";

		public string serverName { get; set; } = "Average's Survival";
		public List<string> broadcastMessages { get; set; } = new List<string> {};

		public int bcInterval { get; set; } = 2;

		public string spawnName { get; set; } = "diemob";

		public static Config Read()
		{
			string filepath = Path.Combine(TShock.SavePath, "AverageTerraria.json");
			try
			{
				Config config = new Config();

				if (File.Exists(filepath))
				{
					config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filepath));
				}

				File.WriteAllText(filepath, JsonConvert.SerializeObject(config, Formatting.Indented));
				return config;
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(ex.ToString());
				return new Config();
			}
		}


	}
}
