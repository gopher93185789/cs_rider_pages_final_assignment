using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core;
using Types;
using System.Text.Json;

namespace cs_rider_pages_final_assignment.Pages.Admin;

public class IndexModel : PageModel {
    private readonly BlogCtx blogCtx;

    public List<Post>? Posts { get; set; }
    public string? UserId { get; set; }

    public IndexModel(BlogCtx ctx) {
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
        }

        return Page();
    }

    public IActionResult OnPostLogout() {
        var token = Request.Cookies["accessToken"];
        if (!string.IsNullOrEmpty(token)) {
            blogCtx.Logout(token);
        }
        Response.Cookies.Delete("accessToken");
        return Redirect("/");
    }

    public IActionResult OnPostDelete(string postId) {
        var token = Request.Cookies["accessToken"];
        if (string.IsNullOrEmpty(token)) {
            return Redirect("/Login");
        }

        var verified = blogCtx.VerifySession(token, out string? err, out string role, out string? userId);
        if (!verified || role != "admin") {
            return Redirect("/Login");
        }

        blogCtx.AdminDeletePost(userId!, postId, out string? delErr);

        return RedirectToPage();
    }
}
