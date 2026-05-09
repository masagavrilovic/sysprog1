using System.Text.Json;

namespace sysprog1
{
    public class Cache
    {
        private static readonly Dictionary<string, CacheEntry> _cache = new();
        private static readonly object _lock = new();
        private static readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);
        private static HttpClient _httpClient;
        public Cache(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public List<Article> Get(string key, string api_key)
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_cache.TryGetValue(key, out CacheEntry? value))
                    {
                        if (value.IsReady)
                        {
                            Logger.Log($"[CACHE HIT]: {key}");
                            return value.Articles!;
                        }
                        if (value.IsLoading)
                        {
                            Logger.Log($"Nit ceka na rezultat za {key}");
                            Monitor.Wait(_lock);
                            continue;
                        }
                        Logger.Log($"[CACHE EXPIRED]: {key}");
                        _cache.Remove(key);
                    }
                    _cache[key] = new CacheEntry { IsLoading = true };
                }

                Logger.Log($"[CACHE MISS]: {key}");
                List<Article> articles;
                try
                {
                    articles = FetchFromApi(key, api_key);
                    lock (_lock)
                    {
                        _cache[key] = new CacheEntry
                        {
                            Articles = articles,
                            ExpiresAt = DateTime.Now.Add(_cacheTtl),
                            IsLoading = false
                        };
                        Monitor.PulseAll(_lock);
                        return articles;
                    }
                }
                catch(Exception e)
                {
                    lock (_lock)
                    {
                        _cache.Remove(key);
                        Monitor.PulseAll(_lock);
                    }
                    throw;
                }  
            }
            
        }

        public static List<Article> FetchFromApi(string query, string api_key)
        {
            var nytUrl = $"https://api.nytimes.com/svc/search/v2/articlesearch.json?q={query}&api-key={api_key}";
            var jsonString = _httpClient.GetStringAsync(nytUrl).Result;
            var doc = JsonDocument.Parse(jsonString);
            var response = doc.RootElement.GetProperty("response");

            if (!response.TryGetProperty("docs", out JsonElement docs) || docs.ValueKind != JsonValueKind.Array)
            {
                return new List<Article>();
            }

            var articles = new List<Article>();
            foreach (var item in docs.EnumerateArray())
            {
                articles.Add(new Article
                {
                    Headline = item.GetProperty("headline").GetProperty("main").GetString(),
                    Abstract = item.GetProperty("abstract").GetString(),
                    WebUrl = item.GetProperty("web_url").GetString(),
                    Date = item.GetProperty("pub_date").GetString()
                });
            }
            return articles;
        }
    }
}
