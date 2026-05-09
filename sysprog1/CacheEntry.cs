namespace sysprog1
{
    public class CacheEntry
    {
        public List<Article>? Articles { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsLoading { get; set; }
        public bool IsReady => !IsLoading && DateTime.Now < ExpiresAt;
    }
}
