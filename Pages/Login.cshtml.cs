using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core;

namespace cs_rider_pages_final_assignment.Pages;

public class LoginModel : PageModel {
    private readonly BlogCtx blogCtx;

    [BindProperty]
    public string? Username { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public string? ErrorMessage { get; set; }

    public LoginModel(BlogCtx ctx) {
        blogCtx = ctx;
    }

    public void OnGet() {
        // check if already logged in
        var token = Request.Cookies["accessToken"];
        if (!string.IsNullOrEmpty(token)) {
            var verified = blogCtx.VerifySession(token, out string? err, out string role, out string? userId);
            if (verified && role == "admin") {
                Response.Redirect("/Admin");
            }
        }
    }

    public IActionResult OnPost() {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password)) {
            ErrorMessage = "Please provide username and password";
            return Page();
        }

        var success = blogCtx.Login(Username, Password, out string? err, out var usr);
        if (!success || usr == null) {
            ErrorMessage = err ?? "Login failed";
            return Page();
        }

        if (usr.role != "admin") {
            ErrorMessage = "Access denied. Admin only.";
            return Page();
        }

        Response.Cookies.Append("accessToken", usr.accessToken, new CookieOptions {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(24)
        });

        return Redirect("/Admin");
    }
}
