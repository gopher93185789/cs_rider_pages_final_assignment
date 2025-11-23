using Microsoft.AspNetCore.Mvc.RazorPages;
using Core;
using Types;
using System.Text.Json;

namespace cs_rider_pages_final_assignment.Pages;

public class PostModel : PageModel {
    private readonly BlogCtx blogCtx;

    public Post? Post { get; set; }
    public string? NewComment { get; set; }
    public string? ErrorMessage { get; set; }

    public PostModel(BlogCtx ctx) {
        blogCtx = ctx;
    }

    public void OnGet(string id) {
        var success = blogCtx.UserGetPosts(null, out string? err, out string? postsJson);
        if (success && !string.IsNullOrEmpty(postsJson)) {
            var posts = JsonSerializer.Deserialize<List<Post>>(postsJson);
            Post = posts?.FirstOrDefault(p => p.PostID == id);
        }
    }

    public void OnPost(string id, string comment) {
        if (string.IsNullOrWhiteSpace(comment)) {
            ErrorMessage = "Comment cannot be empty";
            OnGet(id);
            return;
        }

        var success = blogCtx.UserAddCommentOnPost(id, comment, out string? err);
        if (!success) {
            ErrorMessage = err;
        }

        OnGet(id);
    }
}
