using cache;

class Program {
    static void Main() {
        TTLCache ch = new TTLCache(TimeSpan.FromSeconds(10));
        ch.Method();
    }
}