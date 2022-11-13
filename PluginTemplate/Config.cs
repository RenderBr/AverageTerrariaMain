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
		public string infoMessage { get; set; } = "Insert text here!";

		public DateTime startDate { get; set; } = DateTime.UtcNow;
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

				if (!File.Exists(filepath))
				{
					File.WriteAllText(filepath, JsonConvert.SerializeObject(config, Formatting.Indented));
				}
				config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filepath));

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
