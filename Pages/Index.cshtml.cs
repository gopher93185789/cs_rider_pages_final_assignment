using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core;
using Types;
using System.Text.Json;

namespace cs_rider_pages_final_assignment.Pages;

public class IndexModel : PageModel {
    private readonly BlogCtx blogCtx;

    public List<Post>? Posts { get; set; }
    public string? SearchQuery { get; set; }

    public IndexModel(BlogCtx ctx) {
        blogCtx = ctx;
    }

    public void OnGet(string? search) {
        SearchQuery = search;

        var success = blogCtx.UserGetPosts(search, out string? err, out string? postsJson);
        if (success && !string.IsNullOrEmpty(postsJson)) {
            Posts = JsonSerializer.Deserialize<List<Post>>(postsJson);
        }
        else {
            Console.WriteLine($"Failed to get posts. Success: {success}, Error: {err}, PostsJson: {postsJson}");
        }
    }
}
