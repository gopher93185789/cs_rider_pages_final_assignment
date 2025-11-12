using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace jwt {
    public class JwtCtx {
        private readonly string secret;

        public JwtCtx(string secret) {
            this.secret = secret;
        }

        public string GenerateToken(string userid, string role, TimeSpan expiry) {
            var header = new {
                alg = "HS256",
                typ = "JWT"
            };

            var payload = new {
                sub = userid,
                role = role,
                exp = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds()
            };

            var headerJson = JsonSerializer.Serialize(header);
            var payloadJson = JsonSerializer.Serialize(payload);

            var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            var signatureInput = $"{headerBase64}.{payloadBase64}";
            var signature = GenerateSignature(signatureInput);

            return $"{signatureInput}.{signature}";
        }

        public bool Validate(string token, out string role, out string userid) {
            userid = "";
            role = "";

            try {
                var parts = token.Split('.');
                if (parts.Length != 3) {
                    return false;
                }

                var headerBase64 = parts[0];
                var payloadBase64 = parts[1];
                var signature = parts[2];

                var signatureInput = $"{headerBase64}.{payloadBase64}";
                var expectedSignature = GenerateSignature(signatureInput);

                if (signature != expectedSignature) {
                    return false;
                }

                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadBase64));
                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);

                if (payload == null) {
                    return false;
                }

                if (payload.TryGetValue("exp", out var expElement)) {
                    var exp = expElement.GetInt64();
                    var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                    if (DateTimeOffset.UtcNow >= expirationTime) {
                        return false;
                    }
                }

                if (payload.TryGetValue("sub", out var subElement)) {
                    userid = subElement.GetString() ?? "";
                }

                if (payload.TryGetValue("role", out var roleElement)) {
                    role = roleElement.GetString() ?? "";
                }

                return true;
            }
            catch {
                return false;
            }
        }

        private string GenerateSignature(string input) {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var inputBytes = Encoding.UTF8.GetBytes(input);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(inputBytes);
            return Base64UrlEncode(hashBytes);
        }

        private static string Base64UrlEncode(byte[] input) {
            var base64 = Convert.ToBase64String(input);
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static byte[] Base64UrlDecode(string input) {
            var base64 = input.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4) {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}
