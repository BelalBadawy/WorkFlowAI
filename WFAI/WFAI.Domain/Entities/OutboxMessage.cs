namespace WFAI.Domain.Entities
{
    /// <summary>
    /// Stores application notifications for asynchronous, reliable dispatch.
    /// </summary>
    public class OutboxMessage : BaseEntity<long>
    {
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime OccurredOnUtc { get; set; }
        public DateTime? ProcessedOnUtc { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryOnUtc { get; set; }
        public string? Error { get; set; }
    }
}