using jwt;

class Program {
    static void Main() {
        var c = new JwtCtx("dibvihsbvibvihebv");
        string tken = c.GenerateToken("usr", "rizzman", TimeSpan.FromHours(1));
        Console.WriteLine(tken);

        var ok = c.Validate(tken, out string role, out string uid);
        if (!ok) {
            Console.WriteLine("invalid");
            return;
        }

        Console.WriteLine(role);
        Console.WriteLine(uid);
    }
}