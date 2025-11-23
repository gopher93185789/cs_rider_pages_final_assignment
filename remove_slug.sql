-- Remove slug column from posts table if it exists
ALTER TABLE posts DROP COLUMN IF EXISTS slug;
