using cache;

class Program {
    static void Main() {
        var c = new TTLCache(TimeSpan.FromMilliseconds(100));
        var ok = c.Add("hello", "world", TimeSpan.FromSeconds(1));
        if (!ok) {
            Console.WriteLine("failed to add key to cache");
            return;
        }

        ok = c.Get("hello", out string? v);
        if (!ok) {
            Console.WriteLine("failed to delete cache");
            return;
        }

        Console.WriteLine(v);
    }
}