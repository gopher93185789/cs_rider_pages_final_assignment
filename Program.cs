using cache;

class Program {
    static void Main() {
        TTLCache c = new TTLCache(TimeSpan.FromSeconds(10));
        c.Add("hello", "world", TimeSpan.FromSeconds(100));
        var ok = c.Get("hello", out string? v);
        if (!ok) {
            Console.WriteLine("failed to get cache entry");
        }
        Console.WriteLine(v);

        c.Delete("hello");

        ok = c.Get("hello", out v);
        if (ok) {
            Console.WriteLine("failed to delete cache");
        }
    }
}