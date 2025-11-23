using Microsoft.AspNetCore.Mvc.RazorPages;
using Core;
using Types;
using System.Text.Json;

namespace cs_rider_pages_final_assignment.Pages;

public class TagsModel : PageModel {
    private readonly BlogCtx blogCtx;

    public List<Post>? Posts { get; set; }
    public string? SelectedTag { get; set; }
    public List<string> AllTags { get; set; } = new List<string>();

    public TagsModel(BlogCtx ctx) {
        blogCtx = ctx;
    }

    public void OnGet(string? tag) {
        SelectedTag = tag;

        if (!string.IsNullOrEmpty(tag)) {
            var tagList = new List<string> { tag };
            var success = blogCtx.UserFilterPostsByTags(tagList, out string? err, out string? postsJson);
            if (success && !string.IsNullOrEmpty(postsJson)) {
                Posts = JsonSerializer.Deserialize<List<Post>>(postsJson);
            }
        }
        else {
            var success = blogCtx.UserGetPosts(null, out string? err, out string? postsJson);
            if (success && !string.IsNullOrEmpty(postsJson)) {
                var allPosts = JsonSerializer.Deserialize<List<Post>>(postsJson);
                Posts = allPosts;
            }
        }

        if (Posts != null) {
            foreach (var post in Posts) {
                var publishedDraft = post.Drafts?.FirstOrDefault(d => d.DraftState == "published" && !d.IsDeleted);
                if (publishedDraft?.Tags != null) {
                    foreach (var t in publishedDraft.Tags) {
                        if (!AllTags.Contains(t)) {
                            AllTags.Add(t);
                        }
                    }
                }
            }
            AllTags.Sort();
        }
    }
}
