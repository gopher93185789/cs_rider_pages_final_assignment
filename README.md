# cs_rider_pages_final_assignment

## Req

- [x] In memory key value store with expiry
- [x] auth - login / logout for admins only
  - [x] auth token in cookie
- [x] Blog posts
  - [x] user role can read posts and add comments but they are not visible to other users yet
  - [x] admin can create posts and drafts
  - [x] admin can update posts
  - [x] admin can delete posts
  - [x] admin needs to approve user comments to be visible under post
  - [x] post needs to have tags, title, body, image
  - [x] post can have muliple versions. one published and muli draft
    - [x] posts scheduler
    - [x] admin can set when a post will be posted/visible to users
    - [x] users can filter posts by tags (client-side filtering)
- [x] full text search posts (can be done in postgres)
- [x] navigate through posts using slugs /posts/:id
- [x] Frontend with Razor Pages
  - [x] minimal dark theme design
  - [x] home page with client-side search and tag filtering
  - [x] individual post pages with comments
  - [x] admin login page
  - [x] admin dashboard for managing posts
  - [x] admin page for creating/editing posts
  - [x] admin page for moderating comments
  - [x] sitemap.xml generation
