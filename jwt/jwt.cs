using Microsoft.AspNetCore.DataProtection;

namespace jwt {
    public class JwtCtx {
        private readonly string secret;

        public JwtCtx(string secret) {
            this.secret = secret;
        }

        public string GenerateToken(string userid, string role, TimeSpan expiry) {
            
            return "";
        }

        public bool Validate(string token, out string role, out string userid) {
            userid = "";
            role = "";
            return true;
        }
    }
}
