using System.Net.Http.Headers;
using cache;

class Program {
    static void main() {
        cache.TTLCache ch = new cache.TTLCache(new TimeSpan(seconds: 10));
        ch.Method();
    }
}