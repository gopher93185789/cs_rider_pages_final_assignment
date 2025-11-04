using Npgsql;
using jwt;
using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
using cache;

namespace core {
    public class UserObject {
        public readonly string username;
        public readonly string role;
        public readonly string accessToken;

        public UserObject(string username, string role, string accessToken) {
            this.username = username;
            this.role = role;
            this.accessToken = accessToken;
        }
    }

    public class BlogCtx {
        private readonly string dsn;
        private readonly NpgsqlConnection conn;
        private readonly JwtCtx jwt;
        private readonly TTLCache cache;

        public BlogCtx(string dsn, JwtCtx jwt, TTLCache cache) {
            this.dsn = dsn;
            this.conn = new NpgsqlConnection(dsn);
            this.jwt = jwt;
            this.cache = cache;
        }

        public bool RegisterUser(string username, string password, out string err) {
            if (username.Length < 3) {
                err = "username cannot be less than 3 characters";
                return false;
            }

            if (password.Length < 3) {
                err = "password cannot be less than 3 characters";
                return false;
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);


            try {
                var cmd = new NpgsqlCommand("INSERT INTO users (username, password_hash) VALUES (@username, @hash)", conn);
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("hash", passwordHash);
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23505") {
                err = "user already exists";
                return false;
            }
            catch (Exception ex) {
                err = ex.Message;
                return false;
            }


            err = "";
            return true;
        }


        public bool Login(string username, string password, out string? err, out UserObject? usr) {
            if (username.Length < 3) {
                err = "invalid login";
                usr = null;
                return false;
            }

            if (password.Length < 3) {
                err = "invalid login";
                usr = null;
                return false;
            }


            try {
                var cmd = new NpgsqlCommand("SELECT role, password_hash, id FROM users WHERE username = @username", conn);
                cmd.Parameters.AddWithValue("username", username);
                var reader = cmd.ExecuteReader();

                if (!reader.Read()) {
                    err = "internal server error";
                    usr = null;
                    return false;
                }

                string role = reader.GetString(0);
                string passwordHash = reader.GetString(1);
                string uid = reader.GetString(2);

                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash)) {
                    err = "invalid login";
                    usr = null;
                    return false;
                }


                string token = jwt.GenerateToken(uid, TimeSpan.FromHours(1));

                var ust = new UserObject(username, role, token);


                err = null;
                usr = ust;
                return true;
            }
            catch {
                err = "an unexpected error occured";
                usr = null;
                return false;
            }
        }

        public bool Logout(string token) {
            string uid;
            var ok = jwt.Validate(token, out uid);
            if (!ok) {
                return false;
            }

            cache.Add(token, uid, TimeSpan.FromHours(1));
            return true;
        }
        public bool VerifySession(string token, out string? err, out string? uid) {
            bool ok = jwt.Validate(token, out uid);
            if (!ok) {
                err = "token is invalid";
                uid = null;
                return false;
            }

            err = null;
            return true;
        }


    }
}
