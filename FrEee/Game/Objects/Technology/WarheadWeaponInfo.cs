﻿using System;

namespace FrEee.Game.Objects.Technology
{
	[Serializable]
	public class WarheadWeaponInfo : WeaponInfo
	{
		public override Enumerations.WeaponTypes WeaponType
		{
			get
			{
				if (IsPointDefense)
					return Enumerations.WeaponTypes.WarheadPointDefense;
				else
					return Enumerations.WeaponTypes.Warhead;
			}
		}
	}
}