using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SocialSync.Models;
using System.Text;

namespace SocialSync.Controllers
{
    public class FriendManagementController : Controller
    {
        private readonly HttpClient _http;

        public FriendManagementController(IHttpClientFactory clientFactory, IConfiguration config)
        {
            _http = clientFactory.CreateClient();

            var supabaseUrl = config["Supabase:Url"];
            var supabaseKey = config["Supabase:AnonKey"];

            _http.BaseAddress = new Uri($"{supabaseUrl}/rest/v1/");
            _http.DefaultRequestHeaders.Add("apikey", supabaseKey);
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
        }

        // =========================
        // HELPER: CURRENT USER ID
        // =========================
        private async Task<Guid?> GetCurrentUserId()
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(email))
                return null;

            var response = await _http.GetAsync(
                $"sosial_sync_users?email=eq.{email}&select=id"
            );

            var json = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<User>>(json);

            return users?.FirstOrDefault()?.Id;
        }

        // =========================
        // INDEX — MY FRIENDS ONLY
        // =========================
        // =========================
        // INDEX — LIST MY FRIENDS ONLY
        // =========================
        public async Task<IActionResult> Index(string? searchString)
        {
            var myId = await GetCurrentUserId();

            if (myId == null)
                return RedirectToAction("Login", "Auth");

            // 1️⃣ Get my friend IDs
            var friendLinkResponse = await _http.GetAsync(
                $"sosial_sync_friends?user_id=eq.{myId}&select=friend_id"
            );

            if (!friendLinkResponse.IsSuccessStatusCode)
                return View(new List<User>());

            var friendLinksJson = await friendLinkResponse.Content.ReadAsStringAsync();
            var friendLinks = JsonConvert.DeserializeObject<List<dynamic>>(friendLinksJson) ?? new();

            var friendIds = friendLinks
                .Select(f => (string)f.friend_id)
                .ToList();

            if (!friendIds.Any())
                return View(new List<User>());

            // 2️⃣ Build user query
            var idFilter = $"id=in.({string.Join(",", friendIds)})";

            var endpoint = string.IsNullOrEmpty(searchString)
                ? $"sosial_sync_users?select=*&{idFilter}"
                : $"sosial_sync_users?select=*&{idFilter}" +
                  $"&or=(fullname.ilike.*{searchString}*,email.ilike.*{searchString}*)";

            var response = await _http.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
                return View(new List<User>());

            var json = await response.Content.ReadAsStringAsync();
            var friends = JsonConvert.DeserializeObject<List<User>>(json) ?? new List<User>();

            return View(friends);
        }


        // =========================
        // DETAILS (VIEW MODAL)
        // =========================
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty)
                return NotFound();

            var response = await _http.GetAsync(
                $"sosial_sync_users?id=eq.{id}&select=*"
            );

            var json = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<User>>(json);

            if (users == null || users.Count == 0)
                return NotFound();

            return PartialView("_DetailsModal", users[0]);
        }

        // =========================
        // DELETE FRIEND (CONFIRM MODAL)
        // =========================
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
                return NotFound();

            var response = await _http.GetAsync(
                $"sosial_sync_users?id=eq.{id}&select=*"
            );

            var json = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<User>>(json);

            if (users == null || users.Count == 0)
                return NotFound();

            return PartialView("_DeleteModal", users[0]);
        }

        // =========================
        // DELETE FRIEND (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var myId = await GetCurrentUserId();

            if (myId == null || id == Guid.Empty)
            {
                TempData["Error"] = "Invalid action.";
                return RedirectToAction("Index");
            }

            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"sosial_sync_friends?user_id=eq.{myId}&friend_id=eq.{id}"
            );

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Failed to remove friend.";
                return RedirectToAction("Index");
            }

            TempData["Success"] = "Friend removed successfully.";
            return RedirectToAction("Index");
        }

        // =========================
        // FIND FRIEND (MODAL)
        // =========================
        public async Task<IActionResult> Find(string? search)
        {
            var myId = await GetCurrentUserId();
            var myEmail = HttpContext.Session.GetString("UserEmail");

            if (myId == null || string.IsNullOrEmpty(myEmail))
                return Unauthorized();

            // 1️⃣ Get my existing friend IDs first
            var friendIdResponse = await _http.GetAsync(
                $"sosial_sync_friends?user_id=eq.{myId}&select=friend_id"
            );

            var friendIdsJson = await friendIdResponse.Content.ReadAsStringAsync();
            var friendLinks = JsonConvert.DeserializeObject<List<dynamic>>(friendIdsJson) ?? new();

            var friendIds = friendLinks
                .Select(f => (string)f.friend_id)
                .ToList();

            // 2️⃣ Build filter safely (NO subquery)
            var excludeIds = friendIds.Any()
                ? $"&id=not.in.({string.Join(",", friendIds)})"
                : "";

            var endpoint = string.IsNullOrEmpty(search)
                ? $"sosial_sync_users?select=*&email=neq.{myEmail}{excludeIds}"
                : $"sosial_sync_users?select=*&email=neq.{myEmail}{excludeIds}" +
                  $"&or=(fullname.ilike.*{search}*,email.ilike.*{search}*)";

            var response = await _http.GetAsync(endpoint);

            // 🔥 IMPORTANT: check status BEFORE deserializing
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine("[SUPABASE ERROR] " + err);
                return PartialView("_FindFriendModal", new List<User>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<User>>(json) ?? new List<User>();

            return PartialView("_FindFriendModal", users);
        }


        // =========================
        // ADD FRIEND (INSTANT)
        // =========================
        [HttpPost]
        public async Task<IActionResult> Follow(Guid id)
        {
            var myId = await GetCurrentUserId();

            if (myId == null || myId == id)
                return BadRequest();

            var payload = new
            {
                user_id = myId,
                friend_id = id
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "sosial_sync_friends"
            )
            {
                Content = content
            };

            request.Headers.Add("Prefer", "resolution=ignore-duplicates");

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return BadRequest();

            return Ok();
        }
    }
}
