﻿using FrEee.Game.Objects.LogMessages;
using System.Collections.Generic;

namespace FrEee.Game.Interfaces
{
	/// <summary>
	/// A type of recycle behavior (scrap, analyze, etc.)
	/// </summary>
	public interface IRecycleBehavior
	{
		string Verb { get; }

		void Execute(IRecyclable target, bool didRecycle = false);

		IEnumerable<LogMessage> GetErrors(IMobileSpaceObject executor, IRecyclable target);
	}
}