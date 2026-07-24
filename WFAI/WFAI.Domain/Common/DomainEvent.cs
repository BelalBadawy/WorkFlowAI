namespace WFAI.Domain.Common
{
    /// <summary>
    /// Base class for domain events emitted by entities. Tracks when the event
    /// occurred and whether it has been published by an event dispatcher.
    /// </summary>
    public abstract class DomainEvent : IDomainEvent
    {
        protected DomainEvent()
        {
            DateOccurred = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Flag indicating whether the event has been published.
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// Timestamp when the event occurred (UTC).
        /// </summary>
        public DateTimeOffset DateOccurred { get; protected set; }
    }
}