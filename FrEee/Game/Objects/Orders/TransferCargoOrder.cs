﻿using FrEee.Game.Interfaces;
using FrEee.Game.Objects.Civilization;
using FrEee.Game.Objects.LogMessages;
using FrEee.Game.Objects.Space;
using FrEee.Utility;
using FrEee.Utility.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace FrEee.Game.Objects.Orders
{
    /// <summary>
    /// An order to transfer cargo from one object to another.
    /// </summary>
    public class TransferCargoOrder : IOrder<ICargoTransferrer>
    {
        #region Public Constructors

        public TransferCargoOrder(bool isLoadOrder, CargoDelta cargoDelta, ICargoTransferrer target)
        {
            Owner = Empire.Current;
            IsLoadOrder = isLoadOrder;
            CargoDelta = cargoDelta;
            Target = target;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// The cargo delta, which specifies what cargo is to be transferred.
        /// </summary>
        public CargoDelta CargoDelta { get; set; }

        public bool ConsumesMovement
        {
            get { return false; }
        }

        public long ID { get; set; }

        public bool IsComplete
        {
            get;
            private set;
        }

        public bool IsDisposed { get; set; }

        /// <summary>
        /// The empire which issued the order.
        /// </summary>
        [DoNotSerialize]
        public Empire Owner { get { return owner; } set { owner = value; } }

        /// <summary>
        /// The cargo transferrer to which the cargo will be transferred, or null to launch/recover to/from space.
        /// </summary>
        [DoNotSerialize]
        public ICargoTransferrer Target { get { return target?.Value; } set { target = value.ReferViaGalaxy(); } }

        #endregion Public Properties

        #region Private Properties

        /// <summary>
        /// True if this is a load order, false if it is a drop order.
        /// </summary>
        private bool IsLoadOrder { get; set; }

        private GalaxyReference<Empire> owner { get; set; }
        private GalaxyReference<ICargoTransferrer> target { get; set; }

        #endregion Private Properties

        #region Public Methods

        public bool CheckCompletion(ICargoTransferrer v)
        {
            return IsComplete;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            foreach (var v in Galaxy.Current.Referrables.OfType<ICargoTransferrer>())
                v.RemoveOrder(this);
            Galaxy.Current.UnassignID(this);
        }

        public void Execute(ICargoTransferrer executor)
        {
            var errors = GetErrors(executor);
            if (executor.Owner != null)
            {
                foreach (var error in errors)
                    executor.Owner.Log.Add(error);
            }

            if (!errors.Any())
            {
                if (IsLoadOrder)
                    Target.TransferCargo(CargoDelta, executor, executor.Owner);
                else
                    executor.TransferCargo(CargoDelta, Target, executor.Owner);
            }
            IsComplete = true;
        }

        public IEnumerable<LogMessage> GetErrors(ICargoTransferrer executor)
        {
            if (Target != null && executor.Sector != Target.Sector)
                yield return executor.CreateLogMessage(executor + " cannot transfer cargo to " + Target + " because they are not in the same sector.");
        }

        public void ReplaceClientIDs(IDictionary<long, long> idmap, ISet<IPromotable> done = null)
        {
            if (done == null)
                done = new HashSet<IPromotable>();
            if (!done.Contains(this))
            {
                done.Add(this);
                target?.ReplaceClientIDs(idmap, done);
                owner.ReplaceClientIDs(idmap, done);
            }
        }

        public override string ToString()
        {
            if (Target == null)
            {
                if (IsLoadOrder)
                    return "Recover " + CargoDelta;
                else
                    return "Launch " + CargoDelta;
            }
            else
            {
                if (IsLoadOrder)
                    return "Load " + CargoDelta + " from " + Target;
                else
                    return "Drop " + CargoDelta + " at " + Target;
            }
        }

        #endregion Public Methods
    }
}
