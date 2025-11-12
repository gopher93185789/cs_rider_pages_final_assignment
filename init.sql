CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(255) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role VARCHAR(10) CHECK (role IN ('admin', 'user')) DEFAULT 'user'
);

CREATE TABLE IF NOT EXISTS posts (
    id SERIAL PRIMARY KEY,
    created_by INT REFERENCES users(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    publish_date TIMESTAMP DEFAULT NOW(),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS comments (
    id SERIAL PRIMARY KEY,
    post_id INT REFERENCES posts(id) ON DELETE CASCADE,
    comment TEXT,
    status VARCHAR(16) CHECK (status IN ('pending', 'approved'))
);

CREATE TABLE IF NOT EXISTS posts_drafts (
    id SERIAL PRIMARY KEY,
    post_id INT REFERENCES posts(id) ON DELETE CASCADE,
    is_deleted BOOLEAN DEFAULT false,
    state VARCHAR(50) CHECK (state IN ('draft', 'published')),
    body TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS posts_draft_tags (
    post_draft_id INT REFERENCES posts_drafts(id) ON DELETE CASCADE,
    tag_name VARCHAR(50)
);

CREATE TABLE IF NOT EXISTS posts_draft_assets (
    id SERIAL PRIMARY KEY,
    post_draft_id INT REFERENCES posts_drafts(id) ON DELETE CASCADE,
    asset_type VARCHAR(10) NOT NULL,
    data BYTEA
);