namespace MixVel.Cache
{
    public class CacheSettings
    {
        public int InvalidationDelayMinInSeconds { get; set; }
        public int InvalidationDelayMaxInSeconds { get; set; }
        public int TimeBucketRangeInMin { get; set; }
        public int MinRoutesToInvalidate { get; set; }
    }
}