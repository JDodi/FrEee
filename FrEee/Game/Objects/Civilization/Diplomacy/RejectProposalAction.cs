﻿using FrEee.Utility;
using FrEee.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrEee.Game.Objects.Civilization.Diplomacy
{
	/// <summary>
	/// An action that rejects a proposal.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class RejectProposalAction : Action
	{
		public RejectProposalAction(Proposal proposal)
			: base(proposal.Executor)
		{
			Proposal = proposal;
		}

		private Reference<Proposal> proposal { get; set; }

		/// <summary>
		/// The proposal in question.
		/// </summary>
		[DoNotSerialize]
		public Proposal Proposal { get { return proposal; } set { proposal = value; } }

		public override void ReplaceClientIDs(IDictionary<long, long> idmap)
		{
			proposal.ReplaceClientIDs(idmap);
		}

		public override string Description
		{
			get { return "Reject " + Proposal; }
		}

		public override void Execute()
		{
			if (Proposal.IsResolved)
				Executor.Log.Add(Target.CreateLogMessage("The proposal \"" + Proposal + "\" has already been resolved and cannot be rejected now."));
			else
				Target.Log.Add(Executor.CreateLogMessage("The " + Executor + " has rejected our proposal (" + Proposal + ")."));
		}
	}
}