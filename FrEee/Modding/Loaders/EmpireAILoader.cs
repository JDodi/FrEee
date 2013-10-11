﻿using FrEee.Game.Objects.Civilization;
using FrEee.Game.Objects.Space;
using FrEee.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FrEee.Game.Objects.AI;

namespace FrEee.Modding.Loaders
{
	/// <summary>
	/// Loads empire AIs.
	/// </summary>
	public class EmpireAILoader : ILoader
	{
		public EmpireAILoader(string modPath)
		{
			FileName = "AI.script";
			ModPath = modPath;
		}

		public void Load(Mod mod)
		{
			string empsFolder;
			if (ModPath == null)
				empsFolder = Path.Combine("Pictures", "Races");
			else
				empsFolder = Path.Combine(ModPath, "Pictures", "Races");
			foreach (var empFolder in Directory.GetDirectories(empsFolder))
			{
				var script = Script.Load(Path.Combine(empFolder, "AI"));
				if (script == null)
					continue; // script does not exist for this shipset
				var ministers = new SafeDictionary<string, ICollection<string>>();
				string curCategory = "Uncategorized";
				var ministersFile = Path.Combine(empFolder, "AI.ministers");
				if (File.Exists(ministersFile))
				{
					foreach (var line in File.ReadAllLines(ministersFile))
					{
						if (line.StartsWith("\t"))
						{
							// found a minister name
							var ministerName = line.Substring(1);
							if (ministers[curCategory] == null)
								ministers[curCategory] = new List<string>();
							ministers[curCategory].Add(ministerName);
						}
						else
						{
							// found a minister category
							curCategory = line;
						}
					}
				}
				var ai = new AI<Empire, Galaxy>(Path.GetFileName(empFolder), script, ministers);
				mod.EmpireAIs.Add(ai);
			}
		}

		public string ModPath
		{
			get;
			set;
		}

		public string FileName
		{
			get;
			set;
		}
	}
}