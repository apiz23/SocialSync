using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Newtonsoft.Json;

public class ProfileController : Controller
{
    private readonly HttpClient _http;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    public ProfileController(IHttpClientFactory clientFactory, IConfiguration config)
    {
        _http = clientFactory.CreateClient();
        _supabaseUrl = config["Supabase:Url"];
        _supabaseKey = config["Supabase:AnonKey"];

        _http.BaseAddress = new Uri($"{_supabaseUrl}/rest/v1/");
        _http.DefaultRequestHeaders.Add("apikey", _supabaseKey);
        _http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _supabaseKey);
    }

    public async Task<IActionResult> Index()
    {
        var email = HttpContext.Session.GetString("UserEmail");
        if (string.IsNullOrEmpty(email))
        {
            TempData["Error"] = "You must log in first.";
            return RedirectToAction("Login", "Auth");
        }

        var response = await _http.GetAsync($"sosial_sync_users?email=eq.{email}&select=*");

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Unable to fetch profile.";
            return RedirectToAction("Index", "Home");
        }

        var json = await response.Content.ReadAsStringAsync();
        var users = JsonConvert.DeserializeObject<List<dynamic>>(json);

        if (users.Count == 0)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Index", "Home");
        }

        var user = users[0];

        ViewData["FullName"] = user.fullname;
        ViewData["Email"] = user.email;

        return View();
    }
}
