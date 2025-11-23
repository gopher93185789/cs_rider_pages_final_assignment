using jwt;
using Core;
using cache;
using Types;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}


var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "dibvihsbvibvihebv";
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? "Host=localhost;Database=blog;Username=postgres;Password=postgres";

var jwtCtx = new JwtCtx(jwtSecret);
var cacheCtx = new TTLCache(TimeSpan.FromMinutes(1));
var blogCtx = new BlogCtx(dbConnectionString, jwtCtx, cacheCtx);


app.MapPost("/api/register", (RegisterRequest req) => {
    var success = blogCtx.RegisterUser(req.Username, req.Password, out string err);
    if (!success) {
        return Results.BadRequest(new { error = err });
    }
    return Results.Ok(new { message = "User registered successfully" });
});

// Login
app.MapPost("/api/login", (LoginRequest req, HttpContext context) => {
    var success = blogCtx.Login(req.Username, req.Password, out string? err, out UserObject? usr);
    if (!success) {
        return Results.BadRequest(new { error = err });
    }

    // Set cookie with the access token
    context.Response.Cookies.Append("accessToken", usr!.accessToken, new CookieOptions {
        HttpOnly = true,
        Secure = false, // Set to true in production with HTTPS
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddHours(24)
    });

    return Results.Ok(new { username = usr.username, role = usr.role });
});

// Logout
app.MapPost("/api/logout", (HttpContext context) => {
    var token = context.Request.Cookies["accessToken"];
    if (string.IsNullOrEmpty(token)) {
        return Results.BadRequest(new { error = "No token provided" });
    }

    var success = blogCtx.Logout(token);

    // Clear the cookie
    context.Response.Cookies.Delete("accessToken");

    if (!success) {
        return Results.BadRequest(new { error = "Invalid token" });
    }
    return Results.Ok(new { message = "Logged out successfully" });
});

// Verify Session
app.MapGet("/api/verify", (HttpRequest request) => {
    var token = request.Cookies["accessToken"];
    if (string.IsNullOrEmpty(token)) {
        return Results.BadRequest(new { error = "No token provided" });
    }

    var success = blogCtx.VerifySession(token, out string? err, out string role, out string? uid);
    if (!success) {
        return Results.BadRequest(new { error = err });
    }
    return Results.Ok(new { userId = uid, role = role });
});

// Admin Get Posts
app.MapGet("/api/admin/posts", (HttpRequest request) => {
    var token = request.Cookies["accessToken"];
    if (string.IsNullOrEmpty(token)) {
        return Results.BadRequest(new { error = "No token provided" });
    }

    var verified = blogCtx.VerifySession(token, out string? verifyErr, out string role, out string? userId);
    if (!verified || role != "admin") {
        return Results.Unauthorized();
    }

    var success = blogCtx.AdminGetPosts(userId!, out string? err, out string? postsJson);
    if (!success) {
        return Results.BadRequest(new { error = err });
    }
    return Results.Ok(JsonSerializer.Deserialize<object>(postsJson!));
});

// User Get Posts
app.MapGet("/api/posts", (string? search) => {
    var success = blogCtx.UserGetPosts(search, out string? err, out string? postsJson);
    if (!success) {
        return Results.BadRequest(new { error = err });
    }
    return Results.Ok(JsonSerializer.Deserialize<object>(postsJson!));
});

// User Add Comment on Post
app.MapPost("/api/posts/{postId}/comments", (string postId, CommentRequest req) => {
    var success = blogCtx.UserAddCommentOnPost(postId, req.Comment, out string? err);
    if (!success) {
        return Results.BadRequest(new { error = err });
    }
    return Results.Ok(new { message = "Comment added successfully" });
});

// Admin Update Comment Status
app.MapPut("/api/admin/comments/{commentId}", (HttpRequest request, string commentId, UpdateCommentStatusRequest req) => {
    var token = request.Cookies["accessToken"];
    if (string.IsNullOrEmpty(token)) {
        return Results.BadRequest(new { error = "No token provided" });
    }

    var verified = blogCtx.VerifySession(token, out string? verifyErr, out string role, out string? userId);
    if (!verified || role != "admin") {
        return Results.Unauthorized();
    }

    var success = blogCtx.AdminUpdateCommentStatus(commentId, req.IsApproved, out string? err);
    if (!success) {
        return Results.BadRequest(new { error = err });
    }
    return Results.Ok(new { message = "Comment status updated successfully" });
});

// Admin Create Post
app.MapPost("/api/admin/posts", (HttpRequest request, CreatePostRequest req) => {
    var token = request.Cookies["accessToken"];
    if (string.IsNullOrEmpty(token)) {
        return Results.BadRequest(new { error = "No token provided" });
    }

    var verified = blogCtx.VerifySession(token, out string? verifyErr, out string role, out string? userId);
    if (!verified || role != "admin") {
        return Results.Unauthorized();
    }

    var success = blogCtx.AdminCreatePost(userId!, req.Title, req.PublishDate, out string? err, out string? postID);
    if (!success) {
        return Results.BadRequest(new { error = err });
    }
    return Results.Ok(new { postId = postID, message = "Post created successfully" });
});

// Admin Create Draft on Post
app.MapPost("/api/admin/posts/{postId}/drafts", (HttpRequest request, string postId, CreateDraftRequest req) => {
    var token = request.Cookies["accessToken"];
    if (string.IsNullOrEmpty(token)) {
        return Results.BadRequest(new { error = "No token provided" });
    }

    var verified = blogCtx.VerifySession(token, out string? verifyErr, out string role, out string? userId);
    if (!verified || role != "admin") {
        return Results.Unauthorized();
    }

    var success = blogCtx.AdminCreatDraftOnPost(userId!, postId, req.IsPublished, req.Body, req.Tags, req.Assets, out string? err);
    if (!success) {
        return Results.BadRequest(new { error = err });
    }
    return Results.Ok(new { message = "Draft created successfully" });
});

app.Run();

// Request/Response DTOs
record RegisterRequest(string Username, string Password);
record LoginRequest(string Username, string Password);
record CommentRequest(string Comment);
record UpdateCommentStatusRequest(bool IsApproved);
record CreatePostRequest(string Title, DateTime PublishDate);
record CreateDraftRequest(bool IsPublished, string Body, List<string> Tags, List<Asset> Assets);