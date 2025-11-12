# cs_rider_pages_final_assignment

## Req

- [x] In memory key value store with expiry
- [x] auth - login / logout for admins only
  - [ ] auth token in cookie
- [ ] Blog posts
  - [x] user role can read posts and add comments but they are not visible to other users yet
  - [x] admin can create posts and drafts
  - [ ] admin can update posts
  - [ ] admin can delete posts
  - [x] admin needs to approve user comments to be visible under post
  - [x] post needs to have  tags, title, body, image
  - [x] post can have muliple versions. one published and muli draft
    - [x] posts scheduler
    - [ ] admin can set when a post will be posted/visible to users
    - [ ] users can filter posts by tags
- [ ] full text search posts (can be done in postgres)
- [ ] navigate through posts useg slugs /posts/:id
