﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Radical.CQRS.Reflection;

namespace Radical.CQRS
{
    public abstract class Aggregate : IAggregate, IEquatable<IAggregate> //, IHaveState<TState>
    {
        protected internal abstract Guid Id { get; set; }
        protected internal virtual int Version { get; set; }

		[Timestamp]
		protected internal virtual byte[] RowVersion { get; set; }

        [NotMapped]
        int IAggregate.Version { get { return this.Version; } }
        [NotMapped]
        Guid IAggregate.Id { get { return this.Id; } }

        [NotMapped]
        public bool IsChanged { get { return this._uncommittedEvents.Any(); } }

	    readonly List<IDomainEvent> _uncommittedEvents = new List<IDomainEvent>();

        protected Aggregate()
        {
            this.Id = Guid.NewGuid();
        }

        IEnumerable<IDomainEvent> IAggregate.GetUncommittedEvents()
        {
            return this._uncommittedEvents.ToArray();
        }

        void IAggregate.ClearUncommittedEvents()
        {
            this._uncommittedEvents.Clear();
        }

        protected void RaiseEvent<TEvent>(Action<TEvent> builder) where TEvent : IDomainEvent
        {
            var newVersion = this.Version + 1;
            var @event = ConcreteProxyCreator.CreateInsance<TEvent>();
            @event.Id = Guid.NewGuid();
            @event.OccurredAt = DateTimeOffset.Now;
            @event.AggregateId = this.Id;
            @event.AggregateVersion = newVersion;

            builder(@event);

            this._uncommittedEvents.Add(@event);
            this.Version = newVersion;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as IAggregate);
        }

		public virtual bool Equals( IAggregate other )
        {
            return other != null && other.Id == this.Id;
        }
    }
}
