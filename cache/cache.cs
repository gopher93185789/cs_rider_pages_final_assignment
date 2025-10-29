using System.Collections.Concurrent;
using System.Threading;

namespace cache {
    class CacheEntry {
        public string Value { get; set; }

        private DateTime expiry;
        public bool IsExpired => DateTime.Now >= expiry;

        public CacheEntry(string val, TimeSpan TTL) {
            this.Value = val;
            this.expiry = DateTime.Now + TTL;
        }

        public void ExtendTTL(TimeSpan newTTL) {
            this.expiry += newTTL;
        }

    }


    public class TTLCache {
        private TimeSpan CheckInterval;
        private readonly ConcurrentDictionary<string, CacheEntry> Store;
        private CancellationTokenSource ctx;
        private Task worker;

        public TTLCache(TimeSpan cleanInterval) {
            this.CheckInterval = cleanInterval;
            this.Store = new ConcurrentDictionary<string, CacheEntry>();
            ctx = new CancellationTokenSource();
            worker = Task.Run(async () => {
                await this.WorkerJob(ctx.Token);
            }, ctx.Token);

        }

        public void Add(string key, string value, TimeSpan ttl) {
            var v = new CacheEntry(value, ttl);
            var ok = this.Store.TryAdd(key, v);
            if (!ok) {
                throw new Exception(message: "failed to add key to store");
            }
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
            Timer t = new Timer(state => {
                if (ctx.IsCancellationRequested) {
                    return;
                }
                foreach (KeyValuePair<string, CacheEntry> kvp in this.Store) {
                    if (kvp.Value.IsExpired) {
                        this.Store.TryRemove(kvp.Key, out _);
                    }
                }
            }, null, 0, (int)this.CheckInterval.TotalMilliseconds);
        }

        public void StopWorker() {
            ctx.Cancel();
        }

        ~TTLCache() {
            ctx.Cancel();
        }

    }
}





