using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SocialSync.Models;
using System.Text;

public class ProfileController : Controller
{
    private readonly HttpClient _http;

    public ProfileController(IHttpClientFactory clientFactory, IConfiguration config)
    {
        _http = clientFactory.CreateClient();
        var supabaseUrl = config["Supabase:Url"];
        var supabaseKey = config["Supabase:AnonKey"];
        _http.BaseAddress = new Uri($"{supabaseUrl}/rest/v1/");
        _http.DefaultRequestHeaders.Add("apikey", supabaseKey);
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
    }

    // =========================
    // PROFILE VIEW
    // =========================
    public async Task<IActionResult> Index()
    {
        var email = HttpContext.Session.GetString("UserEmail");
        if (string.IsNullOrEmpty(email))
            return RedirectToAction("Login", "Auth");

        var response = await _http.GetAsync(
            $"social_sync_users?email=eq.{email}&select=*"
        );

        var json = await response.Content.ReadAsStringAsync();
        var users = JsonConvert.DeserializeObject<List<User>>(json);

        if (users == null || users.Count == 0)
            return RedirectToAction("Index", "Home");

        return View(users[0]);
    }

    // =========================
    // GET EDIT
    // =========================
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var email = HttpContext.Session.GetString("UserEmail");
        if (string.IsNullOrEmpty(email))
            return RedirectToAction("Login", "Auth");

        var response = await _http.GetAsync(
            $"social_sync_users?email=eq.{email}&select=*"
        );

        var json = await response.Content.ReadAsStringAsync();
        var users = JsonConvert.DeserializeObject<List<User>>(json);

        if (users == null || users.Count == 0)
            return RedirectToAction("Index");

        return View(users[0]);
    }

    // =========================
    // POST EDIT
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(User model)
    {
        if (model.Id == Guid.Empty)
        {
            TempData["Error"] = "Invalid user ID.";
            return View(model);
        }

        var updateData = new
        {
            fullname = model.FullName,
            phone = model.Phone,
            is_active = model.IsActive,
            bio = model.Bio,
            persona1 = model.Persona1,
            persona2 = model.Persona2,
            persona3 = model.Persona3
        };

        var json = JsonConvert.SerializeObject(updateData);
        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"social_sync_users?id=eq.{model.Id}"
        )
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("Prefer", "return=minimal");
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Profile update failed.";
            return View(model);
        }

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction("Index");
    }
}