# cs_rider_pages_final_assignment

## Req

- [ ] In memory key value store with expiry
- [ ] auth - login / logout for admins only
  - [ ] auth token in cookie
- [ ] Blog posts
  - [ ] user role can read posts and add comments but they are not visible to other users yet
  - [ ] admin can create, update, delete posts
  - [ ] admin needs to approve user comments to be visible under post
  - [ ] post needs to have  tags, title, body, image
  - [ ] post can have muliple versions. one published and muli draft support
    - [ ] posts scheduler
    - [ ] admin can set when a post will be posted/visible to users
    - [ ] users can filter posts by tags
- [ ] full text search posts (can be done in postgres)
- [ ] read blog posts
- [ ] navigate through posts useg slugs /posts/:id

```sql
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(255) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role VARCHAR(10) CHECK (role IN ('admin', 'user')) DEFAULT 'user'
);

CREATE TABLE IF NOT EXISTS posts (
    id SERIAL PRIMARY KEY,
    title TEXT NOT NULL,
    publish_date TIMESTAMP DEFAULT NOW(),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS posts_drafts (
    id SERIAL PRIMARY KEY,
    post_id INT REFERENCES posts(id) ON DELETE CASCADE,
    is_deleted BOOLEAN,
    state VARCHAR(50) CHECK (state IN ('draft', 'published')),
    body TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS posts_draft_tags (
    post_draft_id INT REFERNCES posts_drafts(id) ON DELETE CASCADE
    tag_name VARCHAR(50)
);

```
