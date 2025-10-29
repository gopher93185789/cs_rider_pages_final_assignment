namespace cache {
    class CacheEntry {
        public required string Value { get; set; }
        
        private readonly DateTime expiry;
        public bool IsExpired => DateTime.Now > expiry;

        public CacheEntry(string val, TimeSpan TTL) {
            this.Value = val;
            this.expiry = DateTime.Now + TTL;
        }

    }


    public class TTLCache {
        private TimeSpan CheckInterval;

        public TTLCache(TimeSpan cleanInterval) {
            this.CheckInterval = cleanInterval;
        }

        public void Method() {
            Console.WriteLine("hello");
        }

    }
}




