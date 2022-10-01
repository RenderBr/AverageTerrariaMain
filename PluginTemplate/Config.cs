﻿using System;
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
