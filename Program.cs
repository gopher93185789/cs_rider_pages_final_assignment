using cache;

class Program {
    static void Main() {
        TTLCache c = new TTLCache(TimeSpan.FromMilliseconds(100));
        c.Add("hello", "world", TimeSpan.FromSeconds(1));
        Thread.Sleep(2000);
        bool ok = c.Get("hello", out _);
        if (ok) {
            Console.WriteLine("failed to delete cache");
        }
    }
}