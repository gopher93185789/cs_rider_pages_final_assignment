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


    public class Asset {
        public string? AssetType { get; set; }
        public byte[]? Data { get; set; }
    }
    public class Comment {
        public string? commentId { get; set; }
        public string? comment { get; set; }
        public string? status { get; set; }
    }

    public class Draft {
        public string? DraftId { get; set; }
        public bool IsDeleted { get; set; }
        public string? DraftState { get; set; }
        public string? Body { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string[]? Tags { get; set; }
        public List<Asset>? assets { get; set; }


    }

    public class Post {
        public string? PostID { get; set; }
        public string? Title { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<Draft>? Drafts { get; set; }
        public List<Comment>? Comments { get; set; }

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

        private static class SqlQueries {
            public const string GetPostSql = @"
WITH posts AS (
    SELECT id, title, publish_date, created_at, updated_at 
    FROM posts
),

comments_agg AS (
    SELECT 
        post_id,
        ARRAY_AGG(id) AS comment_ids,
        ARRAY_AGG(comment) AS comments,
        ARRAY_AGG(status) AS comment_status
    FROM comments
    GROUP BY post_id
),

drafts_agg AS (
    SELECT 
        post_id,
        ARRAY_AGG(id) AS draft_ids,
        ARRAY_AGG(is_deleted) AS is_deleted,
        ARRAY_AGG(state) AS draft_states,
        ARRAY_AGG(body) AS draft_bodies,
        ARRAY_AGG(created_at) AS draft_created,
        ARRAY_AGG(updated_at) AS draft_updated
    FROM posts_drafts
    GROUP BY post_id
),

draft_tags_agg AS (
    SELECT 
        d.post_id,
        ARRAY_AGG(DISTINCT t.tag_name) AS draft_tags
    FROM posts_draft_tags t
    JOIN posts_drafts d ON t.post_draft_id = d.id
    GROUP BY d.post_id
),

draft_assets_agg AS (
    SELECT 
        d.post_id,
        ARRAY_AGG(a.post_draft_id) AS draft_ids,
        ARRAY_AGG(a.asset_type) AS asset_types,
        ARRAY_AGG(a.data) AS asset_data
    FROM posts_draft_assets a
    JOIN posts_drafts d ON a.post_draft_id = d.id
    GROUP BY d.post_id
)
SELECT 
    p.id,
    p.title,
    p.publish_date,
    p.created_at,
    p.updated_at,
    c.comment_ids,
    c.comments,
    c.comment_status,
    d.draft_ids,
    d.is_deleted,
    d.draft_states,
    d.draft_bodies,
    d.draft_created,
    d.draft_updated,
    t.draft_tags,
    a.draft_ids,
    a.asset_types,
    a.asset_data
FROM posts p
LEFT JOIN comments_agg c ON p.id = c.post_id
LEFT JOIN drafts_agg d ON p.id = d.post_id
LEFT JOIN draft_tags_agg t ON p.id = t.post_id
LEFT JOIN draft_assets_agg a ON p.id = a.post_id;

        ";
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

            NpgsqlCommand? cmd = null;
            try {
                cmd = new NpgsqlCommand("INSERT INTO users (username, password_hash) VALUES (@username, @hash)", conn);
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
            finally {
                cmd?.Dispose();
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


            NpgsqlCommand? cmd = null;
            NpgsqlDataReader? reader = null;
            try {
                cmd = new NpgsqlCommand("SELECT role, password_hash, id FROM users WHERE username = @username", conn);
                cmd.Parameters.AddWithValue("username", username);
                reader = cmd.ExecuteReader();

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
            finally {
                reader?.Close();
                cmd?.Dispose();
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

        public bool GetPosts(out string? err, out List<Post>? posts) {
            NpgsqlCommand? cmd = null;
            NpgsqlDataReader? reader = null;

            string? id = null;
            string? title = null;
            DateTime? publishDate = null;
            DateTime? createdAt = null;
            DateTime? updatedAt = null;
            string[]? commentIds = null;
            string[]? comments = null;
            string[]? commentStatus = null;
            string[]? draftIds = null;
            bool[]? isDeleted = null;
            string[]? draftStates = null;
            string[]? draftBodies = null;
            DateTime[]? draftCreated = null;
            DateTime[]? draftUpdated = null;
            string[]? assetDraftIds = null;
            string[]? draftTags = null;
            string[]? assetTypes = null;
            byte[][]? assetData = null;

            try {
                cmd = new NpgsqlCommand(SqlQueries.GetPostSql, conn);
                reader = cmd.ExecuteReader();

                posts = new List<Post>();
                while (reader.Read()) {
                    id = reader.GetString(0);
                    title = reader.GetString(1);
                    publishDate = reader.GetDateTime(2);
                    createdAt = reader.GetDateTime(3);
                    updatedAt = reader.GetDateTime(4);
                    commentIds = reader.GetFieldValue<string[]>(5);
                    comments = reader.GetFieldValue<string[]>(6);
                    commentStatus = reader.GetFieldValue<string[]>(7);
                    draftIds = reader.GetFieldValue<string[]>(8);
                    isDeleted = reader.GetFieldValue<bool[]>(9);
                    draftStates = reader.GetFieldValue<string[]>(10);
                    draftBodies = reader.GetFieldValue<string[]>(11);
                    draftCreated = reader.GetFieldValue<DateTime[]>(12);
                    draftUpdated = reader.GetFieldValue<DateTime[]>(13);
                    draftTags = reader.GetFieldValue<string[]>(14);
                    assetDraftIds = reader.GetFieldValue<string[]>(15);
                    assetTypes = reader.GetFieldValue<string[]>(16);
                    assetData = reader.GetFieldValue<byte[][]>(17);

                    var p = new Post();
                    p.PostID = id;
                    p.Title = title;
                    p.CreatedAt = createdAt;
                    p.UpdatedAt = updatedAt;

                    p.Drafts = new List<Draft>();
                    p.Comments = new List<Comment>();

                    for (int i = 0; i < commentIds.Length; i++) {
                        var c = new Comment();
                        c.commentId = commentIds[i];
                        c.comment = comments[i];
                        c.status = commentStatus[i];

                        p.Comments.Add(c);
                    }


                    for (int i = 0; i < draftIds.Length; i++) {
                        var d = new Draft();
                        d.DraftId = draftIds[i];
                        d.IsDeleted = isDeleted[i];
                        d.DraftState = draftStates[i];
                        d.Body = draftBodies[i];
                        d.CreatedAt = draftCreated[i];
                        d.UpdatedAt = draftUpdated[i];
                        d.UpdatedAt = draftUpdated[i];

                        d.Tags = draftTags[i].Split(",");

                        d.assets = new List<Asset>();

                        for (int j = 0; j < assetDraftIds.Length; j++) {
                            if (assetDraftIds[j] == draftIds[i]) {
                                var a = new Asset();
                                a.AssetType = assetTypes[j];
                                a.Data = assetData[j];
                                d.assets.Add(a);
                            }
                        }

                        p.Drafts.Add(d);
                    }

                    posts.Add(p);
                }


                err = "";
                return true;
            }
            catch (Exception e) {
                err = e.Message;
                posts = null;
                return false;
            }
            finally {
                reader?.Close();
                cmd?.Dispose();
            }

        }


    }
}
