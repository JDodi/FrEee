﻿using FrEee.Game.Interfaces;
using FrEee.Game.Objects.Civilization;
using FrEee.Game.Objects.LogMessages;
using FrEee.Game.Objects.Space;
using FrEee.Utility;
using FrEee.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrEee.Game.Objects.Commands
{
	/// <summary>
	/// Adds an order to the end of the queue.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class AddOrderCommand<T> : OrderCommand<T>
		where T : IOrderable
	{
		public AddOrderCommand(Empire issuer, T target, IOrder<T> order)
			: base(issuer, target, order)
		{
			
		}

		public override void Execute()
		{
			if (Issuer == Target.Owner)
			{
				if (Order is IConstructionOrder && ((IConstructionOrder)Order).Item != null)
					Issuer.Log.Add(new GenericLogMessage("You cannot add a construction order with a prefabricated construction item!"));
				else if (Order == null)
					Issuer.Log.Add(new GenericLogMessage("Attempted to add a null order to " + Target + ". This is probably a game bug."));
				else
					Target.AddOrder(Order);
			}
			else
				Issuer.Log.Add(new GenericLogMessage(Issuer + " cannot issue commands to " + Target + " belonging to " + Target.Owner + "!"));
		}

		private IOrder<T> order
		{
			get;
			set;
		}

		public override IOrder<T> Order
		{
			get
			{
				return order;
			}
			set
			{
				base.Order = value;
				order = value;
			}
		}

		public override void ReplaceClientIDs(IDictionary<long, long> idmap)
		{
			base.ReplaceClientIDs(idmap);
			Order.ReplaceClientIDs(idmap);
		}

		public override IEnumerable<IReferrable> NewReferrables
		{
			get
			{
				yield return Order;
			}
		}
	}
}
