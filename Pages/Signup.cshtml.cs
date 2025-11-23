using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Core;

namespace cs_rider_pages_final_assignment.Pages;

public class SignupModel : PageModel {
    private readonly BlogCtx blogCtx;

    [BindProperty]
    public string? Username { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    [BindProperty]
    public string? ConfirmPassword { get; set; }

    public string? ErrorMessage { get; set; }

    public SignupModel(BlogCtx ctx) {
        blogCtx = ctx;
    }

    public void OnGet() {
    }

    public IActionResult OnPost() {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password)) {
            ErrorMessage = "Please provide username and password";
            return Page();
        }

        if (Password != ConfirmPassword) {
            ErrorMessage = "Passwords do not match";
            return Page();
        }

        if (Password.Length < 3) {
            ErrorMessage = "Password must be at least 3 characters";
            return Page();
        }

        if (Username.Length < 3) {
            ErrorMessage = "Username must be at least 3 characters";
            return Page();
        }

        var success = blogCtx.RegisterUser(Username, Password, out string err);
        if (!success) {
            ErrorMessage = err;
            return Page();
        }

        // auto login after signup
        var loginSuccess = blogCtx.Login(Username, Password, out string? loginErr, out var usr);
        if (loginSuccess && usr != null) {
            Response.Cookies.Append("accessToken", usr.accessToken, new CookieOptions {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(24)
            });

            return Redirect("/Admin");
        }

        return Redirect("/Login");
    }
}
