﻿using FrEee.Game.Interfaces;
using FrEee.Game.Objects.Civilization;
using FrEee.Game.Objects.Combat2;
using FrEee.Utility;
using FrEee.WinForms.DataGridView;
using FrEee.WinForms.Interfaces;
using FrEee.WinForms.MogreCombatRender;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FrEee.WinForms.Forms
{
	public partial class BattleResultsForm : Form, IBindable<Battle_Space>
	{
		public BattleResultsForm(Battle_Space battle)
		{
			InitializeComponent();

			Bind(battle);

            try { this.Icon = new Icon(FrEee.WinForms.Properties.Resources.FrEeeIcon); }
            catch { }
		}

		/// <summary>
		/// The battle we are displaying results for.
		/// TODO - create an IBattle interface so we can have pluggable combat systems
		/// </summary>
		public Battle_Space Battle { get; private set; }

		public void Bind(Battle_Space data)
		{
			Battle = data;
			Bind();
		}

		public void Bind()
		{
			// create grid config
			var cfg = new GridConfig();
			cfg.Name = "Default";
			cfg.Columns.Add(new GridColumnConfig("EmpireIcon", "Flag", typeof(DataGridViewImageColumn), Color.White, Format.Raw));
			cfg.Columns.Add(new GridColumnConfig("EmpireName", "Empire", typeof(DataGridViewTextBoxColumn), Color.White, Format.Raw, Sort.Ascending, 1));
			cfg.Columns.Add(new GridColumnConfig("HullIcon", "Icon", typeof(DataGridViewImageColumn), Color.White, Format.Raw));
			cfg.Columns.Add(new GridColumnConfig("HullName", "Hull", typeof(DataGridViewTextBoxColumn), Color.White, Format.Raw));
			cfg.Columns.Add(new GridColumnConfig("HullSize", "Size", typeof(DataGridViewTextBoxColumn), Color.White, Format.Kilotons, Sort.Descending, 2));
			cfg.Columns.Add(new GridColumnConfig("StartCount", "Start #", typeof(DataGridViewTextBoxColumn), Color.White, Format.UnitsBForBillions));
			cfg.Columns.Add(new GridColumnConfig("StartHP", "Start HP", typeof(DataGridViewTextBoxColumn), Color.White, Format.UnitsBForBillions));
			cfg.Columns.Add(new GridColumnConfig("Losses", "Losses", typeof(DataGridViewTextBoxColumn), Color.White, Format.UnitsBForBillions));
			cfg.Columns.Add(new GridColumnConfig("Damage", "Damage", typeof(DataGridViewTextBoxColumn), Color.White, Format.UnitsBForBillions));
			grid.CurrentGridConfig = cfg;

			// run through sim to get stats
			//Battle.Resolve();

			// gather grid data
			var data = new List<object>();
			var combatants = Battle.StartCombatants.Join(Battle.EndCombatants, kvp => kvp.Key, kvp => kvp.Key, (kvpStart, kvpEnd) => new { Start = kvpStart.Value, End = kvpEnd.Value });
			foreach (var group in combatants.GroupBy(c => new CombatantInfo
				{
					Empire = c.Start.Owner,
					HullIcon = GetHullIcon(c.Start),
					HullName = GetHullName(c.Start),
					HullSize = GetHullSize(c.Start)
				}))
			{
				var count = group.Count();
				var hp = group.Sum(c => c.Start.ArmorHitpoints + c.Start.HullHitpoints);
				var item = new
				{
					EmpireIcon = group.Key.Empire.Icon,
					EmpireName = group.Key.Empire.Name,
					HullIcon = group.Key.HullIcon,
					HullName = group.Key.HullName,
					HullSize = group.Key.HullSize,
					StartCount = count,
					StartHP = hp,
					Losses = group.Count(c => c.End.IsDestroyed || c.End.Owner != c.Start.Owner), // destroyed or captured
					Damage = hp - group.Sum(c =>
						{
							return c.End.ArmorHitpoints + c.End.HullHitpoints;
						})
				};
				data.Add(item);
			}
			grid.Data = data;
			grid.Initialize();
		}

		private class CombatantInfo
		{
			public Empire Empire { get; set; }
			public Image HullIcon { get; set; }
			public string HullName { get; set; }
			public int HullSize { get; set; }

			public override bool Equals(object obj)
			{
				if (!(obj is CombatantInfo))
					return false;
				var ci = (CombatantInfo)obj;
				return Empire == ci.Empire && HullName == ci.HullName && HullSize == ci.HullSize;
			}

			public override int GetHashCode()
			{
				return HashCodeMasher.Mash(Empire, HullName, HullSize);
			}
		}

		/// <summary>
		/// Gets the icon of a vehicle/design, or a generic icon if it's not a vehicle/design (e.g. if it's a planet).
		/// </summary>
		/// <param name="obj"></param>
		private static Image GetHullIcon(IOwnable obj)
		{
			if (obj is IVehicle)
				return ((IVehicle)obj).Design.Hull.GetIcon(obj.Owner.ShipsetPath);
			if (obj is IDesign)
				return ((IDesign)obj).Hull.GetIcon(obj.Owner.ShipsetPath);
			return Pictures.GetGenericImage(obj.GetType());
		}

		/// <summary>
		/// Gets the hull name of a vehicle/design, or the type name if it's not a vehicle/design (e.g. if it's a planet).
		/// </summary>
		/// <param name="obj"></param>
		private static string GetHullName(IOwnable obj)
		{
			if (obj is IVehicle)
				return ((IVehicle)obj).Design.Hull.Name;
			if (obj is IDesign)
				return ((IDesign)obj).Hull.Name;
			return obj.GetType().Name;
		}

		/// <summary>
		/// Gets the hull size of a vehicle/design, or int.MaxValue if it's not a vehicle/design (e.g. if it's a planet).
		/// </summary>
		/// <param name="obj"></param>
		private static int GetHullSize(IOwnable obj)
		{
			if (obj is IVehicle)
				return ((IVehicle)obj).Design.Hull.Size;
			if (obj is IDesign)
				return ((IDesign)obj).Hull.Size;
			return int.MaxValue;
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void btnReplay_Click(object sender, EventArgs e)
		{
			MogreFreeMain replay = new MogreFreeMain(Battle);
		}
	}
}
