using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core;
using Types;
using System.Text.Json;

namespace cs_rider_pages_final_assignment.Pages.Admin;

public class EditPostModel : PageModel {
    private readonly BlogCtx blogCtx;

    [BindProperty]
    public string? PostId { get; set; }

    [BindProperty]
    public string? Title { get; set; }

    [BindProperty]
    public DateTime PublishDate { get; set; } = DateTime.Now;

    [BindProperty]
    public string? Body { get; set; }

    [BindProperty]
    public string? Tags { get; set; }

    [BindProperty]
    public bool IsPublished { get; set; } = true;

    [BindProperty]
    public string? DraftId { get; set; }

    public Post? ExistingPost { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UserId { get; set; }
    public EditPostModel(BlogCtx ctx) {
        blogCtx = ctx;
    }

    public IActionResult OnGet(string? id) {
        var token = Request.Cookies["accessToken"];
        if (string.IsNullOrEmpty(token)) {
            return Redirect("/Login");
        }

        var verified = blogCtx.VerifySession(token, out string? err, out string role, out string? userId);
        if (!verified || role != "admin") {
            return Redirect("/Login");
        }

        UserId = userId;

        if (!string.IsNullOrEmpty(id)) {
            var success = blogCtx.AdminGetPosts(userId!, out string? postsErr, out string? postsJson);
            if (success && !string.IsNullOrEmpty(postsJson)) {
                var posts = JsonSerializer.Deserialize<List<Post>>(postsJson);
                ExistingPost = posts?.FirstOrDefault(p => p.PostID == id);

                if (ExistingPost != null) {
                    PostId = ExistingPost.PostID;
                    Title = ExistingPost.Title;
                    PublishDate = ExistingPost.CreatedAt ?? DateTime.Now;

                    // Load the published draft if it exists, otherwise the latest draft
                    var publishedDraft = ExistingPost.Drafts?.FirstOrDefault(d => d.DraftState == "published" && !d.IsDeleted);
                    var latestDraft = publishedDraft ?? ExistingPost.Drafts?.OrderByDescending(d => d.UpdatedAt).FirstOrDefault();

                    if (latestDraft != null) {
                        DraftId = latestDraft.DraftId;
                        Body = latestDraft.Body;
                        Tags = latestDraft.Tags != null ? string.Join(", ", latestDraft.Tags) : "";
                        IsPublished = latestDraft.DraftState == "published";
                    }
                }
            }
        }

        return Page();
    }

    public IActionResult OnPost() {
        var token = Request.Cookies["accessToken"];
        if (string.IsNullOrEmpty(token)) {
            return Redirect("/Login");
        }

        var verified = blogCtx.VerifySession(token, out string? err, out string role, out string? userId);
        if (!verified || role != "admin") {
            return Redirect("/Login");
        }

        UserId = userId;

        if (string.IsNullOrWhiteSpace(Title)) {
            ErrorMessage = "Title is required";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Body)) {
            ErrorMessage = "Body is required";
            return Page();
        }

        var tagsList = string.IsNullOrWhiteSpace(Tags)
            ? new List<string>()
            : Tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

        var assets = new List<Asset>();

        if (string.IsNullOrEmpty(PostId)) {
            // create new post
            var createSuccess = blogCtx.AdminCreatePost(userId!, Title, PublishDate, out string? createErr, out string? newPostId);
            if (!createSuccess) {
                ErrorMessage = createErr;
                return Page();
            }

            PostId = newPostId;

            var draftSuccess = blogCtx.AdminCreatDraftOnPost(userId!, PostId!, IsPublished, Body, tagsList, assets, out string? draftErr);
            if (!draftSuccess) {
                ErrorMessage = draftErr;
                return Page();
            }
        }
        else {
            // update existing post
            var updateSuccess = blogCtx.AdminUpdatePost(userId!, PostId, Title, PublishDate, out string? updateErr);
            if (!updateSuccess) {
                ErrorMessage = updateErr;
                return Page();
            }

            // Update existing draft instead of creating a new one
            if (!string.IsNullOrEmpty(DraftId)) {
                var draftSuccess = blogCtx.AdminUpdateDraft(userId!, PostId, DraftId, IsPublished, Body, tagsList, out string? draftErr);
                if (!draftSuccess) {
                    ErrorMessage = draftErr;
                    return Page();
                }
            }
            else {
                // No draft exists, create a new one
                var draftSuccess = blogCtx.AdminCreatDraftOnPost(userId!, PostId, IsPublished, Body, tagsList, assets, out string? draftErr);
                if (!draftSuccess) {
                    ErrorMessage = draftErr;
                    return Page();
                }
            }
        }
        return Redirect("/Admin");
    }
}
