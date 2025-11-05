using System.Collections.Concurrent;
using System.ComponentModel;

namespace cache {
    class CacheEntry(string val, TimeSpan TTL) {
        public string Value { get; set; } = val;
        private DateTime expiry = DateTime.UtcNow + TTL;
        public bool IsExpired => DateTime.UtcNow >= expiry;

        public void ExtendTTL(TimeSpan newTTL) {
            this.expiry += newTTL;
        }

    }

    public interface ITTLCache {

        bool Add(string key, string value, TimeSpan ttl);
        bool Get(string key, out string? value);
        bool Delete(string key);
        bool ExtendTTL(string key, TimeSpan newTTL);
        void ClearCache();
        void StopWorker();

    }


    public class TTLCache : ITTLCache {
        private readonly TimeSpan CheckInterval;
        private readonly ConcurrentDictionary<string, CacheEntry> Store;
        private readonly CancellationTokenSource ctx;
        private readonly Task worker;

        public TTLCache(TimeSpan cleanInterval) {
            this.CheckInterval = cleanInterval;
            this.Store = new ConcurrentDictionary<string, CacheEntry>();
            ctx = new CancellationTokenSource();
            worker = Task.Run(async () => {
                await this.WorkerJob(ctx.Token);
            }, ctx.Token);
        }

        public bool Add(string key, string value, TimeSpan ttl) {
            var v = new CacheEntry(value, ttl);
            var ok = this.Store.TryAdd(key, v);
            if (!ok) {
                return false;
            }

            return true;
        }

        public bool Get(string key, out string? value) {
            var ok = this.Store.TryGetValue(key, out CacheEntry? v);
            if (!ok || v == null || v.IsExpired) {
                value = null;
                return false;
            }

            value = v.Value;
            return true;
        }

        public bool Delete(string key) {
            return this.Store.TryRemove(key, out _);
        }

        public bool ExtendTTL(string key, TimeSpan newTTL) {
            var ok = this.Store.TryGetValue(key, out CacheEntry? v);
            if (!ok || v == null) {
                return false;
            }

            v.ExtendTTL(newTTL);
            this.Store[key] = v;
            return true;
        }

        public void ClearCache() {
            this.Store.Clear();
        }

        private async Task WorkerJob(CancellationToken ctx) {
            while (!ctx.IsCancellationRequested) {
                foreach (KeyValuePair<string, CacheEntry> kvp in this.Store) {
                    if (kvp.Value.IsExpired) {
                        this.Store.TryRemove(kvp.Key, out _);
                    }
                }

                await Task.Delay(CheckInterval, ctx);
            }
        }

        public void StopWorker() {
            ctx.Cancel();
        }

        ~TTLCache() {
            ctx.Cancel();
            ctx.Dispose();
        }

    }
}





