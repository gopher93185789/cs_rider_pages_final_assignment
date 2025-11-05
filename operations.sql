WITH posts AS (
    SELECT id, title, publish_date, created_at, updated_at 
    FROM posts 
), 
drafts AS (
    SELECT d.id, d.post_id, d.is_deleted, d.state, d.body, d.created_at, d.updated_at
    FROM posts_drafts d 
    LEFT JOIN posts p ON d.post_id = p.id
),
draft_tags AS (
    SELECT post_draft_id, STRING_AGG(t.tag_name, ',') AS tags
    FROM posts_draft_tags t
    LEFT JOIN drafts d ON t.post_draft_id = d.id
    GROUP BY d.id,t.post_draft_id
),
draft_assets AS (
    SELECT a.post_draft_id, a.asset_type, a.data 
    FROM posts_draft_assets a 
    LEFT JOIN drafts d ON a.post_draft_id = d.id
)
SELECT 
    p.id,
    p.title,
    p.publish_date,
    p.created_at,
    p.updated_at,

    ARRAY_AGG(d.post_id) AS draft_ids,
    ARRAY_AGG(d.is_deleted) AS is_deleted,
    ARRAY_AGG(d.state) AS draft_states,
    ARRAY_AGG(d.body) AS draft_bodies ,
    ARRAY_AGG(d.created_at) AS draft_created,
    ARRAY_AGG(d.updated_at) AS draft_updated,

    ARRAY_AGG(t.tags) AS draft_tags,

    ARRAY_AGG(a.post_draft_id) as draft_ids,
    ARRAY_AGG(a.asset_type) as asset_types,
    ARRAY_AGG(a.data) AS asset_data
FROM posts p
LEFT JOIN drafts d ON p.id = d.post_id
LEFT JOIN draft_tags t ON d.id = t.post_draft_id 
LEFT JOIN posts_draft_assets a ON d.id = a.post_draft_id
GROUP BY 
    p.id, p.title, p.publish_date, p.created_at, p.updated_at

