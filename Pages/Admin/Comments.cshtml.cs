using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core;
using Types;
using System.Text.Json;

namespace cs_rider_pages_final_assignment.Pages.Admin;

public class CommentsModel : PageModel {
    private readonly BlogCtx blogCtx;

    public List<Post>? Posts { get; set; }
    public string? UserId { get; set; }

    public CommentsModel(BlogCtx ctx) {
        blogCtx = ctx;
    }

    public IActionResult OnGet() {
        var token = Request.Cookies["accessToken"];
        if (string.IsNullOrEmpty(token)) {
            return Redirect("/Login");
        }

        var verified = blogCtx.VerifySession(token, out string? err, out string role, out string? userId);
        if (!verified || role != "admin") {
            return Redirect("/Login");
        }

        UserId = userId;

        var success = blogCtx.AdminGetPosts(userId!, out string? postsErr, out string? postsJson);
        if (success && !string.IsNullOrEmpty(postsJson)) {
            Posts = JsonSerializer.Deserialize<List<Post>>(postsJson);
            Posts = Posts?.Where(p => p.Comments != null && p.Comments.Count > 0).ToList();
        }

        return Page();
    }

    public IActionResult OnPostApprove(string commentId) {
        var token = Request.Cookies["accessToken"];
        if (string.IsNullOrEmpty(token)) {
            return Redirect("/Login");
        }

        var verified = blogCtx.VerifySession(token, out string? err, out string role, out string? userId);
        if (!verified || role != "admin") {
            return Redirect("/Login");
        }

        blogCtx.AdminUpdateCommentStatus(commentId, true, out string? updateErr);

        return RedirectToPage();
    }

    public IActionResult OnPostReject(string commentId) {
        var token = Request.Cookies["accessToken"];
        if (string.IsNullOrEmpty(token)) {
            return Redirect("/Login");
        }

        var verified = blogCtx.VerifySession(token, out string? err, out string role, out string? userId);
        if (!verified || role != "admin") {
            return Redirect("/Login");
        }

        blogCtx.AdminUpdateCommentStatus(commentId, false, out string? updateErr);

        return RedirectToPage();
    }
}
