using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SocialSync.Models;
using System.Net.Http.Headers;
using System.Text;

namespace SocialSync.Controllers
{
    public class PostsController : Controller
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;

        public PostsController(IHttpClientFactory clientFactory, IConfiguration config)
        {
            _http = clientFactory.CreateClient();
            _supabaseUrl = config["Supabase:Url"];
            _supabaseKey = config["Supabase:AnonKey"];

            _http.BaseAddress = new Uri($"{_supabaseUrl}/rest/v1/");
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            _http.DefaultRequestHeaders.Add("Prefer", "return=representation");
        }

        // ============================
        // GET: POSTS LIST
        // ============================
        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _http.GetAsync(
                    "social_sync_posts?select=*&order=created_at.desc"
                );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to load posts.";
                    return View(new List<Post>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var posts = JsonConvert.DeserializeObject<List<Post>>(json) ?? new List<Post>();

                return View(posts);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(new List<Post>());
            }
        }

        // ============================
        // GET: CREATE POST
        // ============================
        public IActionResult Create()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "Please login to create a post.";
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // ============================
        // POST: CREATE POST
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post model)
        {
            ModelState.Remove("Author");
            ModelState.Remove("AuthorId");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                TempData["Error"] = "Validation failed: " + string.Join(", ", errors);
                return View(model);
            }

            var authorEmail = HttpContext.Session.GetString("UserEmail");
            var authorIdRaw = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(authorEmail) || string.IsNullOrEmpty(authorIdRaw))
            {
                TempData["Error"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Auth");
            }

            if (!Guid.TryParse(authorIdRaw, out var authorId))
            {
                TempData["Error"] = "Invalid user session.";
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var postData = new
                {
                    title = model.Title?.Trim(),
                    content = model.Content?.Trim(),
                    author = authorEmail,
                    author_id = authorId,
                    category = string.IsNullOrWhiteSpace(model.Category) ? "General" : model.Category.Trim(),
                    image_url = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim()
                };

                var json = JsonConvert.SerializeObject(postData);
                var request = new HttpRequestMessage(HttpMethod.Post, "social_sync_posts")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("Prefer", "return=representation");
                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Failed to create post: {errorContent}";
                    return View(model);
                }

                TempData["Success"] = "Post created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return View(model);
            }
        }

        // ============================
        // GET: EDIT POST (Page)
        // ============================
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var response = await _http.GetAsync($"social_sync_posts?id=eq.{id}&select=*");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Post not found.";
                    return RedirectToAction("Index");
                }

                var json = await response.Content.ReadAsStringAsync();
                var posts = JsonConvert.DeserializeObject<List<Post>>(json);

                if (posts == null || !posts.Any())
                {
                    TempData["Error"] = "Post not found.";
                    return RedirectToAction("Index");
                }

                var post = posts[0];
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");

                if (post.Author != currentUserEmail)
                {
                    TempData["Error"] = "You don't have permission to edit this post.";
                    return RedirectToAction("Index");
                }

                return View(post);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ============================
        // POST: EDIT POST (Form submission from Edit page)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Edit")]
        public async Task<IActionResult> EditPost(Post model)
        {
            ModelState.Remove("Author");
            ModelState.Remove("AuthorId");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");
                var checkResponse = await _http.GetAsync($"social_sync_posts?id=eq.{model.PostId}&select=author");
                var checkJson = await checkResponse.Content.ReadAsStringAsync();
                var posts = JsonConvert.DeserializeObject<List<Post>>(checkJson);

                if (posts == null || !posts.Any() || posts[0].Author != currentUserEmail)
                {
                    TempData["Error"] = "You don't have permission to edit this post.";
                    return RedirectToAction("Index");
                }

                var payload = new
                {
                    title = model.Title?.Trim(),
                    content = model.Content?.Trim(),
                    category = string.IsNullOrWhiteSpace(model.Category) ? "General" : model.Category.Trim(),
                    image_url = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim()
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PatchAsync($"social_sync_posts?id=eq.{model.PostId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to update post.";
                    return View(model);
                }

                TempData["Success"] = "Post updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }

        // ============================
        // POST: EDIT POST (AJAX from modal)
        // ============================
        [HttpPost]
        [Route("Posts/EditAjax/{id}")]
        public async Task<IActionResult> EditAjax(long id, [FromBody] Post model)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");
                var checkResponse = await _http.GetAsync($"social_sync_posts?id=eq.{id}&select=author");
                var checkJson = await checkResponse.Content.ReadAsStringAsync();
                var posts = JsonConvert.DeserializeObject<List<Post>>(checkJson);

                if (posts == null || !posts.Any() || posts[0].Author != currentUserEmail)
                {
                    return Forbid();
                }

                var payload = new
                {
                    title = model.Title?.Trim(),
                    content = model.Content?.Trim(),
                    category = model.Category ?? "General",
                    image_url = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim()
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PatchAsync($"social_sync_posts?id=eq.{id}", content);

                return response.IsSuccessStatusCode ? Ok() : StatusCode(500);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // ============================
        // GET: DELETE POST (Page)
        // ============================
        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var response = await _http.GetAsync($"social_sync_posts?id=eq.{id}&select=*");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Post not found.";
                    return RedirectToAction("Index");
                }

                var json = await response.Content.ReadAsStringAsync();
                var posts = JsonConvert.DeserializeObject<List<Post>>(json);

                if (posts == null || !posts.Any())
                {
                    TempData["Error"] = "Post not found.";
                    return RedirectToAction("Index");
                }

                var post = posts[0];
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");

                if (post.Author != currentUserEmail)
                {
                    TempData["Error"] = "You don't have permission to delete this post.";
                    return RedirectToAction("Index");
                }

                return View(post);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ============================
        // POST: DELETE POST (Form submission from Delete page)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(long PostId)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");
                var checkResponse = await _http.GetAsync($"social_sync_posts?id=eq.{PostId}&select=author");
                var checkJson = await checkResponse.Content.ReadAsStringAsync();
                var posts = JsonConvert.DeserializeObject<List<Post>>(checkJson);

                if (posts == null || !posts.Any() || posts[0].Author != currentUserEmail)
                {
                    TempData["Error"] = "You don't have permission to delete this post.";
                    return RedirectToAction("Index");
                }

                var response = await _http.DeleteAsync($"social_sync_posts?id=eq.{PostId}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to delete post.";
                    return RedirectToAction("Delete", new { id = PostId });
                }

                TempData["Success"] = "Post deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ============================
        // POST: DELETE POST (AJAX from modal)
        // ============================
        [HttpPost]
        [Route("Posts/DeleteAjax/{id}")]
        public async Task<IActionResult> DeleteAjax(long id)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");
                var checkResponse = await _http.GetAsync($"social_sync_posts?id=eq.{id}&select=author");
                var checkJson = await checkResponse.Content.ReadAsStringAsync();
                var posts = JsonConvert.DeserializeObject<List<Post>>(checkJson);

                if (posts == null || !posts.Any() || posts[0].Author != currentUserEmail)
                {
                    return Forbid();
                }

                var response = await _http.DeleteAsync($"social_sync_posts?id=eq.{id}");
                return response.IsSuccessStatusCode ? Ok() : StatusCode(500);
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}