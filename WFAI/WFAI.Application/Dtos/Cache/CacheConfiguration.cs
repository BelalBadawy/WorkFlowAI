namespace WFAI.Application.Dtos.Cache
{
    /// <summary>
    /// Configuration options for in-memory caching behavior.
    /// </summary>
    public class CacheConfiguration
    {
        /// <summary>
        /// Absolute expiration time expressed in hours. Cached entries will be
        /// removed after this many hours regardless of access.
        /// </summary>
        public int AbsoluteExpirationInHours { get; set; }

        /// <summary>
        /// Sliding expiration interval expressed in minutes. Each access to a cached
        /// entry will renew its lifetime by this amount.
        /// </summary>
        public int SlidingExpirationInMinutes { get; set; }

    }
}