using System.Dynamic;

namespace Types {
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

    public static class SqlQueries {
        public const string GetPostSql = @"
WITH posts AS (
    SELECT id, title, publish_date, created_at, updated_at 
    FROM posts
    WHERE publish_date <= now()
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


        public const string CreateCommentOnPost = @"
        INSERT INTO comments (post_id, comment, status) 
        VALUES (@post_id, @comment, @status);
        ";
        public const string UpdateCommentStatus = @"
  UPDATE comments SET status = @status 
  WHERE id = @comment_id;
        ";
    }

}