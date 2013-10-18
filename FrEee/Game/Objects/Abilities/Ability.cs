using FrEee.Game.Interfaces;
using FrEee.Modding;
using FrEee.Modding.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrEee.Game.Objects.Abilities
{
	/// <summary>
	/// A special ability of some game object, or just a tag used by the AI or by modders.
	/// </summary>
	[Serializable]
	public class Ability : IContainable<object>
	{
		public Ability(object container)
		{
			Container = container;
			Values = new List<Formula<string>>();
		}

		/// <summary>
		/// The name of the ability.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// A description of the ability's effects.
		/// </summary>
		public Formula<string> Description { get; set; }

		/// <summary>
		/// Extra data for the ability.
		/// </summary>
		public IList<Formula<string>> Values { get; set; }

		/// <summary>
		/// The first value of the ability. Not all abilities have values, so this might be null!
		/// </summary>
		public Formula<string> Value1
		{
			get
			{
				return Values.ElementAtOrDefault(0);
			}
		}

		/// <summary>
		/// The second value of the ability. Not all abilities have two values, so this might be null!
		/// </summary>
		public IFormula<string> Value2
		{
			get
			{
				return Values.ElementAtOrDefault(1);
			}
		}

		public override string ToString()
		{
			return Description ?? (Name + ": " + string.Join(", ", Values.Select(v => v.Value).ToArray()));
		}

		public object Container { get; private set; }
	}

	/// <summary>
	/// Rules for grouping abilities with the same name in preparation for stacking.
	/// </summary>
	public enum AbilityGroupingRule
	{
		/// <summary>
		/// Place the abilities all in one big group.
		/// </summary>
		DoNotGroup,
		/// <summary>
		/// Group the abilities by their first value.
		/// </summary>
		GroupByValue1,
		/// <summary>
		/// Group the abilities by their second value.
		/// </summary>
		GroupByValue2,
	}

	/// <summary>
	/// Rules for stacking ability values within a group of similar abilities.
	/// </summary>
	public enum AbilityValueStackingRule
	{
		/// <summary>
		/// Do not stack the values within the group. This is the only valid option for non-numeric values.
		/// Note that this does not necessarily mean that only one instance of the ability will apply!
		/// To guarantee this, use TakeHighest, TakeAverage, or TakeLowest.
		/// </summary>
		DoNotStack,
		/// <summary>
		/// Add the values within the group. Only works properly for numeric values.
		/// </summary>
		Add,
		/// <summary>
		/// Take the highest value within the group. Only works properly for numeric values.
		/// </summary>
		TakeHighest,
		/// <summary>
		/// Take the average of the group values. Only works properly for numeric values.
		/// </summary>
		TakeAverage,
		/// <summary>
		/// Take the lowest value within the group. Only works properly for numeric values.
		/// </summary>
		TakeLowest
	}
}
