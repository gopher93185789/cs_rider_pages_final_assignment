using Npgsql;
using jwt;
using System.Text.Json;
using cache;
using Types;


namespace Core {
    public class BlogCtx {
        private readonly string dsn;
        private readonly NpgsqlConnection conn;
        private readonly JwtCtx jwt;
        private readonly TTLCache cache;

        public BlogCtx(string dsn, JwtCtx jwt, TTLCache cache) {
            this.dsn = dsn;
            this.conn = new NpgsqlConnection(dsn);
            this.conn.Open();
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

            NpgsqlCommand? cmd = null;
            try {
                cmd = new NpgsqlCommand("INSERT INTO users (username, password_hash, role) VALUES (@username, @hash, 'admin')", conn);
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
                    err = "invalid login";
                    usr = null;
                    reader?.Close();
                    return false;
                }

                string role = reader.GetString(0);
                string passwordHash = reader.GetString(1);
                string uid = reader.GetInt32(2).ToString();

                reader.Close();

                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash)) {
                    err = "invalid login";
                    usr = null;
                    return false;
                }


                string token = jwt.GenerateToken(uid, role, TimeSpan.FromHours(1));

                var ust = new UserObject(username, role, token);


                err = null;
                usr = ust;
                return true;
            }
            catch (Exception ex) {
                err = ex.Message;
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
            var ok = jwt.Validate(token, out _, out uid);
            if (!ok) {
                return false;
            }

            cache.Add(token, uid, TimeSpan.FromHours(1));
            return true;
        }
        public bool VerifySession(string token, out string? err, out string role, out string? uid) {
            err = null;
            bool ok = jwt.Validate(token, out role, out uid);
            if (!ok) {
                err = "token is invalid";
                uid = null;
                return false;
            }

            return true;
        }

        public bool AdminGetPosts(string userid, out string? err, out string? postsJson) {
            err = null;

            if (this.cache.Get("admin_posts" + userid, out postsJson)) {
                return true;
            }

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
                cmd = new NpgsqlCommand(SqlQueries.AdminGetPostSql, conn);
                cmd.Parameters.AddWithValue("user_id", int.Parse(userid));
                Console.WriteLine($"AdminGetPosts: Querying posts for user_id={userid}");
                reader = cmd.ExecuteReader();
                Console.WriteLine($"AdminGetPosts: Query executed, reader has rows: {reader.HasRows}");

                var posts = new List<Post>();
                while (reader.Read()) {
                    id = reader.GetInt32(0).ToString();
                    title = reader.GetString(1);
                    publishDate = reader.GetDateTime(2);
                    createdAt = reader.GetDateTime(3);
                    updatedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4);
                    var commentIdsInt = reader.IsDBNull(5) ? null : reader.GetFieldValue<int[]>(5);
                    commentIds = commentIdsInt?.Select(x => x.ToString()).ToArray();
                    comments = reader.IsDBNull(6) ? null : reader.GetFieldValue<string[]>(6);
                    commentStatus = reader.IsDBNull(7) ? null : reader.GetFieldValue<string[]>(7);
                    var draftIdsInt = reader.IsDBNull(8) ? null : reader.GetFieldValue<int[]>(8);
                    draftIds = draftIdsInt?.Select(x => x.ToString()).ToArray();
                    isDeleted = reader.IsDBNull(9) ? null : reader.GetFieldValue<bool[]>(9);
                    draftStates = reader.IsDBNull(10) ? null : reader.GetFieldValue<string[]>(10);
                    draftBodies = reader.IsDBNull(11) ? null : reader.GetFieldValue<string[]>(11);
                    draftCreated = reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTime[]>(12);
                    draftUpdated = reader.IsDBNull(13) ? null : reader.GetFieldValue<DateTime[]>(13);
                    draftTags = reader.IsDBNull(14) ? null : reader.GetFieldValue<string[]>(14);
                    var assetDraftIdsInt = reader.IsDBNull(15) ? null : reader.GetFieldValue<int[]>(15);
                    assetDraftIds = assetDraftIdsInt?.Select(x => x.ToString()).ToArray();
                    assetTypes = reader.IsDBNull(16) ? null : reader.GetFieldValue<string[]>(16);
                    assetData = reader.IsDBNull(17) ? null : reader.GetFieldValue<byte[][]>(17);

                    var p = new Post();
                    p.PostID = id;
                    p.Title = title;
                    p.CreatedAt = createdAt;
                    p.UpdatedAt = updatedAt;

                    p.Drafts = new List<Draft>();
                    p.Comments = new List<Comment>();

                    if (commentIds != null) {
                        for (int i = 0; i < commentIds.Length; i++) {
                            var c = new Comment();
                            c.commentId = commentIds[i];
                            c.comment = comments[i];
                            c.status = commentStatus[i];

                            p.Comments.Add(c);
                        }
                    }

                    if (draftIds != null) {
                        for (int i = 0; i < draftIds.Length; i++) {
                            var d = new Draft();
                            d.DraftId = draftIds[i];
                            d.IsDeleted = isDeleted[i];
                            d.DraftState = draftStates[i];
                            d.Body = draftBodies[i];
                            d.CreatedAt = draftCreated[i];
                            d.UpdatedAt = draftUpdated[i];

                            d.Tags = draftTags != null && i < draftTags.Length ? draftTags[i].Split(",") : new string[0];

                            d.assets = new List<Asset>();

                            if (assetDraftIds != null && assetTypes != null && assetData != null) {
                                for (int j = 0; j < assetDraftIds.Length; j++) {
                                    if (assetDraftIds[j] == draftIds[i]) {
                                        var a = new Asset();
                                        a.AssetType = assetTypes[j];
                                        a.Data = assetData[j];
                                        d.assets.Add(a);
                                    }
                                }
                            }

                            p.Drafts.Add(d);
                        }
                    }

                    posts.Add(p);
                }

                Console.WriteLine($"AdminGetPosts: Found {posts.Count} posts for user {userid}");
                postsJson = JsonSerializer.Serialize(posts);
                this.cache.Add("admin_posts" + userid, postsJson, TimeSpan.FromSeconds(60));
                return true;
            }
            catch (Exception e) {
                err = e.Message;
                postsJson = "";
                return false;
            }
            finally {
                reader?.Close();
                cmd?.Dispose();
            }

        }

        public bool UserGetPosts(string? searchQuery, out string? err, out string? postsJson) {
            err = null;
            // Temporarily disable cache for debugging
            // if (this.cache.Get("posts", out postsJson)) {
            //     return true;
            // }

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
                if (!string.IsNullOrWhiteSpace(searchQuery)) {
                    cmd = new NpgsqlCommand(SqlQueries.SearchSql, conn);
                    cmd.Parameters.AddWithValue("lang", "english");
                    cmd.Parameters.AddWithValue("query", searchQuery);
                    Console.WriteLine("Running search query");
                }
                else {
                    cmd = new NpgsqlCommand(SqlQueries.UserGetPostSql, conn);
                    Console.WriteLine("Running UserGetPostSql");
                }
                reader = cmd.ExecuteReader();
                Console.WriteLine($"Query executed, reader has rows: {reader.HasRows}");

                var posts = new List<Post>();
                while (reader.Read()) {
                    id = reader.GetInt32(0).ToString();
                    title = reader.GetString(1);
                    publishDate = reader.GetDateTime(2);
                    createdAt = reader.GetDateTime(3);
                    updatedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4);
                    var commentIdsInt = reader.IsDBNull(5) ? null : reader.GetFieldValue<int[]>(5);
                    commentIds = commentIdsInt?.Select(x => x.ToString()).ToArray();
                    comments = reader.IsDBNull(6) ? null : reader.GetFieldValue<string[]>(6);
                    commentStatus = reader.IsDBNull(7) ? null : reader.GetFieldValue<string[]>(7);
                    var draftIdsInt = reader.IsDBNull(8) ? null : reader.GetFieldValue<int[]>(8);
                    draftIds = draftIdsInt?.Select(x => x.ToString()).ToArray();
                    isDeleted = reader.IsDBNull(9) ? null : reader.GetFieldValue<bool[]>(9);
                    draftStates = reader.IsDBNull(10) ? null : reader.GetFieldValue<string[]>(10);
                    draftBodies = reader.IsDBNull(11) ? null : reader.GetFieldValue<string[]>(11);
                    draftCreated = reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTime[]>(12);
                    draftUpdated = reader.IsDBNull(13) ? null : reader.GetFieldValue<DateTime[]>(13);
                    draftTags = reader.IsDBNull(14) ? null : reader.GetFieldValue<string[]>(14);
                    var assetDraftIdsInt = reader.IsDBNull(15) ? null : reader.GetFieldValue<int[]>(15);
                    assetDraftIds = assetDraftIdsInt?.Select(x => x.ToString()).ToArray();
                    assetTypes = reader.IsDBNull(16) ? null : reader.GetFieldValue<string[]>(16);
                    assetData = reader.IsDBNull(17) ? null : reader.GetFieldValue<byte[][]>(17);

                    var p = new Post();
                    p.PostID = id;
                    p.Title = title;
                    p.CreatedAt = createdAt;
                    p.UpdatedAt = updatedAt;

                    p.Drafts = new List<Draft>();
                    p.Comments = new List<Comment>();

                    if (commentIds != null) {
                        for (int i = 0; i < commentIds.Length; i++) {
                            var c = new Comment();
                            c.commentId = commentIds[i];
                            c.comment = comments[i];
                            c.status = commentStatus[i];

                            p.Comments.Add(c);
                        }
                    }

                    if (draftIds != null) {
                        for (int i = 0; i < draftIds.Length; i++) {
                            var d = new Draft();
                            d.DraftId = draftIds[i];
                            d.IsDeleted = isDeleted[i];
                            d.DraftState = draftStates[i];
                            d.Body = draftBodies[i];
                            d.CreatedAt = draftCreated[i];
                            d.UpdatedAt = draftUpdated[i];

                            if (draftTags != null && i < draftTags.Length && !string.IsNullOrEmpty(draftTags[i])) {
                                d.Tags = draftTags[i].Split(",").Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
                            }
                            else {
                                d.Tags = new string[0];
                            }

                            d.assets = new List<Asset>();

                            if (assetDraftIds != null && assetTypes != null && assetData != null) {
                                for (int j = 0; j < assetDraftIds.Length; j++) {
                                    if (assetDraftIds[j] == draftIds[i]) {
                                        var a = new Asset();
                                        a.AssetType = assetTypes[j];
                                        a.Data = assetData[j];
                                        d.assets.Add(a);
                                    }
                                }
                            }

                            p.Drafts.Add(d);
                        }
                    }

                    posts.Add(p);
                }

                Console.WriteLine($"UserGetPosts: Found {posts.Count} posts from query");
                foreach (var post in posts) {
                    Console.WriteLine($"  Post: {post.PostID} - {post.Title}, Drafts: {post.Drafts?.Count ?? 0}");
                    if (post.Drafts != null) {
                        foreach (var draft in post.Drafts) {
                            Console.WriteLine($"    Draft: {draft.DraftId}, State: {draft.DraftState}, Deleted: {draft.IsDeleted}");
                        }
                    }
                }
                postsJson = JsonSerializer.Serialize(posts);
                this.cache.Add("posts", postsJson, TimeSpan.FromMinutes(5));
                return true;
            }
            catch (Exception e) {
                Console.WriteLine($"UserGetPosts Error: {e.Message}");
                Console.WriteLine($"Stack Trace: {e.StackTrace}");
                err = e.Message;
                postsJson = "";
                return false;
            }
            finally {
                reader?.Close();
                cmd?.Dispose();
            }

        }
        public bool UserAddCommentOnPost(string postID, string comment, out string? err) {
            err = null;

            if (string.IsNullOrWhiteSpace(postID)) {
                err = "please provide a valid postid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(comment) || comment.Length > 500) {
                err = "please provide a comment that falls withing the range (1-500 characters)";
                return false;
            }

            NpgsqlCommand? cmd = null;
            try {
                cmd = new NpgsqlCommand(SqlQueries.CreateCommentOnPost, conn);
                cmd.Parameters.AddWithValue("post_id", int.Parse(postID));
                cmd.Parameters.AddWithValue("comment", comment);
                cmd.Parameters.AddWithValue("status", "pending");
                cmd.ExecuteNonQuery();

                return true;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                err = "internal server error, please try again later";
                return false;
            }
            finally {
                cmd?.Dispose();
            }
        }

        public bool AdminUpdateCommentStatus(string commentID, bool isApproved, out string? err) {
            err = null;

            if (string.IsNullOrWhiteSpace(commentID)) {
                err = "please provide a valid commentid";
                return false;
            }

            NpgsqlCommand? cmd = null;

            try {
                cmd = new NpgsqlCommand(SqlQueries.UpdateCommentStatus, conn);

                cmd.Parameters.AddWithValue("status", isApproved ? "approved" : "pending");
                cmd.Parameters.AddWithValue("comment_id", int.Parse(commentID));
                cmd.ExecuteNonQuery();

                return true;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                err = "internal server error, please try again later";
                return false;
            }
            finally {
                cmd?.Dispose();
            }
        }

        public bool AdminCreatePost(string userID, string title, DateTime publishDate, out string? err, out string? postID) {
            err = null;
            postID = null;

            if (string.IsNullOrWhiteSpace(userID)) {
                err = "please provide a valid userid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(title)) {
                err = "please provide a valid userid";
                return false;
            }

            NpgsqlCommand? cmd = null;

            try {
                cmd = new NpgsqlCommand(SqlQueries.CreatePost, conn);

                cmd.Parameters.AddWithValue("created_by", int.Parse(userID));
                cmd.Parameters.AddWithValue("title", title);
                cmd.Parameters.AddWithValue("publish_date", publishDate);
                var id = cmd.ExecuteScalar();

                this.cache.Delete("admin_posts" + userID);
                postID = id?.ToString();
                return true;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                err = e.Message;
                return false;
            }
            finally {
                cmd?.Dispose();
            }
        }


        public bool AdminCreatDraftOnPost(string userID, string postId, bool isPublished, string body, List<string> tags, List<Asset> assets, out string? err) {
            err = null;

            if (string.IsNullOrWhiteSpace(userID)) {
                err = "please provide a valid userid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(postId)) {
                err = "please provide a valid postid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(body)) {
                err = "please provide a valid body";
                return false;
            }

            NpgsqlCommand? cmd = null;
            NpgsqlTransaction? transaction = null;

            try {
                transaction = conn.BeginTransaction();

                Console.WriteLine($"AdminCreatDraftOnPost: Creating draft for post {postId}, isPublished={isPublished}");
                cmd = new NpgsqlCommand(SqlQueries.CreateDraftOnPostSql, conn, transaction);
                cmd.Parameters.AddWithValue("post_id", int.Parse(postId));
                var draftState = isPublished ? "published" : "draft";
                Console.WriteLine($"AdminCreatDraftOnPost: Setting state to '{draftState}'");
                cmd.Parameters.AddWithValue("state", draftState);
                cmd.Parameters.AddWithValue("body", body);

                var draftId = cmd.ExecuteScalar();
                if (draftId == null) {
                    transaction.Rollback();
                    err = "failed to create draft";
                    return false;
                }

                int draftIdInt = Convert.ToInt32(draftId);

                if (tags != null && tags.Count > 0) {
                    foreach (var tag in tags) {
                        if (!string.IsNullOrWhiteSpace(tag)) {
                            cmd = new NpgsqlCommand(SqlQueries.CreateDraftTagSql, conn, transaction);
                            cmd.Parameters.AddWithValue("post_draft_id", draftIdInt);
                            cmd.Parameters.AddWithValue("tag_name", tag);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                if (assets != null && assets.Count > 0) {
                    foreach (var asset in assets) {
                        if (asset.Data != null && !string.IsNullOrWhiteSpace(asset.AssetType)) {
                            cmd = new NpgsqlCommand(SqlQueries.CreateDraftAssetSql, conn, transaction);
                            cmd.Parameters.AddWithValue("post_draft_id", draftIdInt);
                            cmd.Parameters.AddWithValue("asset_type", asset.AssetType);
                            cmd.Parameters.AddWithValue("data", asset.Data);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();


                this.cache.Delete("admin_posts" + userID);
                if (isPublished) {
                    this.cache.Delete("posts");
                }

                return true;
            }
            catch (Exception e) {
                transaction?.Rollback();
                Console.WriteLine(e.Message);
                err = e.Message;
                return false;
            }
            finally {
                cmd?.Dispose();
                transaction?.Dispose();
            }
        }

        public bool AdminUpdateDraft(string userID, string postId, string draftId, bool isPublished, string body, List<string> tags, out string? err) {
            err = null;

            if (string.IsNullOrWhiteSpace(userID)) {
                err = "please provide a valid userid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(postId)) {
                err = "please provide a valid postid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(draftId)) {
                err = "please provide a valid draftid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(body)) {
                err = "please provide a valid body";
                return false;
            }

            NpgsqlCommand? cmd = null;
            NpgsqlTransaction? transaction = null;

            try {
                transaction = conn.BeginTransaction();

                Console.WriteLine($"AdminUpdateDraft: Updating draft {draftId} for post {postId}, isPublished={isPublished}");

                // Update the draft
                cmd = new NpgsqlCommand(SqlQueries.UpdateDraftSql, conn, transaction);
                cmd.Parameters.AddWithValue("draft_id", int.Parse(draftId));
                var draftState = isPublished ? "published" : "draft";
                Console.WriteLine($"AdminUpdateDraft: Setting state to '{draftState}'");
                cmd.Parameters.AddWithValue("state", draftState);
                cmd.Parameters.AddWithValue("body", body);
                cmd.ExecuteNonQuery();

                // If publishing this draft, unpublish other drafts
                if (isPublished) {
                    cmd = new NpgsqlCommand(SqlQueries.UnpublishOtherDraftsSql, conn, transaction);
                    cmd.Parameters.AddWithValue("post_id", int.Parse(postId));
                    cmd.Parameters.AddWithValue("draft_id", int.Parse(draftId));
                    cmd.ExecuteNonQuery();
                }

                // Delete existing tags for this draft
                cmd = new NpgsqlCommand(SqlQueries.DeleteDraftTagsSql, conn, transaction);
                cmd.Parameters.AddWithValue("draft_id", int.Parse(draftId));
                cmd.ExecuteNonQuery();

                // Add new tags
                if (tags != null && tags.Count > 0) {
                    foreach (var tag in tags) {
                        if (!string.IsNullOrWhiteSpace(tag)) {
                            cmd = new NpgsqlCommand(SqlQueries.CreateDraftTagSql, conn, transaction);
                            cmd.Parameters.AddWithValue("post_draft_id", int.Parse(draftId));
                            cmd.Parameters.AddWithValue("tag_name", tag);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();

                this.cache.Delete("admin_posts" + userID);
                if (isPublished) {
                    this.cache.Delete("posts");
                }

                return true;
            }
            catch (Exception e) {
                transaction?.Rollback();
                Console.WriteLine($"AdminUpdateDraft Error: {e.Message}");
                err = e.Message;
                return false;
            }
            finally {
                cmd?.Dispose();
                transaction?.Dispose();
            }
        }

        public bool AdminUpdatePost(string userID, string postId, string title, DateTime publishDate, out string? err) {
            err = null;

            if (string.IsNullOrWhiteSpace(userID)) {
                err = "please provide a valid userid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(postId)) {
                err = "please provide a valid postid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(title)) {
                err = "please provide a valid title";
                return false;
            }

            NpgsqlCommand? cmd = null;

            try {
                cmd = new NpgsqlCommand(SqlQueries.UpdatePostSql, conn);
                cmd.Parameters.AddWithValue("post_id", int.Parse(postId));
                cmd.Parameters.AddWithValue("title", title);
                cmd.Parameters.AddWithValue("publish_date", publishDate);
                var result = cmd.ExecuteScalar();

                if (result == null) {
                    err = "post not found";
                    return false;
                }

                this.cache.Delete("admin_posts" + userID);
                this.cache.Delete("posts");
                return true;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                err = "internal server error, please try again later";
                return false;
            }
            finally {
                cmd?.Dispose();
            }
        }

        public bool AdminDeletePost(string userID, string postId, out string? err) {
            err = null;

            if (string.IsNullOrWhiteSpace(userID)) {
                err = "please provide a valid userid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(postId)) {
                err = "please provide a valid postid";
                return false;
            }

            NpgsqlCommand? cmd = null;

            try {
                cmd = new NpgsqlCommand(SqlQueries.DeletePostSql, conn);
                cmd.Parameters.AddWithValue("post_id", int.Parse(postId));
                cmd.ExecuteNonQuery();

                this.cache.Delete("admin_posts" + userID);
                this.cache.Delete("posts");
                return true;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                err = "internal server error, please try again later";
                return false;
            }
            finally {
                cmd?.Dispose();
            }
        }

        public bool UserFilterPostsByTags(List<string> tags, out string? err, out string? postsJson) {
            err = null;
            postsJson = null;

            if (tags == null || tags.Count == 0) {
                err = "please provide at least one tag";
                return false;
            }

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
                cmd = new NpgsqlCommand(SqlQueries.FilterPostsByTagsSql, conn);
                cmd.Parameters.AddWithValue("tags", tags.ToArray());
                reader = cmd.ExecuteReader();

                var posts = new List<Post>();
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

                postsJson = JsonSerializer.Serialize(posts);
                return true;
            }
            catch (Exception e) {
                err = e.Message;
                postsJson = "";
                return false;
            }
            finally {
                reader?.Close();
                cmd?.Dispose();
            }
        }
    }
}
