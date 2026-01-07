using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using BCrypt.Net;

public class AuthController : Controller
{
    private readonly HttpClient _http;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    public AuthController(IHttpClientFactory clientFactory, IConfiguration config)
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

    // =========================
    // GET LOGIN
    // =========================
    [HttpGet]
    public IActionResult Login() => View();

    // =========================
    // POST LOGIN
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string Email, string Password)
    {
        var response = await _http.GetAsync(
            $"social_sync_users?email=eq.{Email}&select=id,email,password"
        );

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Login failed. Try again.";
            return View();
        }

        var json = await response.Content.ReadAsStringAsync();
        var users = JsonConvert.DeserializeObject<List<dynamic>>(json);

        if (users == null || users.Count == 0)
        {
            TempData["Error"] = "Email not found.";
            return View();
        }

        var user = users[0];

        // ✅ CORRECT PROPERTY ACCESS (LOWERCASE)
        string storedHash = user.password;
        bool valid = BCrypt.Net.BCrypt.Verify(Password, storedHash);

        if (!valid)
        {
            TempData["Error"] = "Incorrect password.";
            return View();
        }

        // ✅ STORE SESSION CORRECTLY
        HttpContext.Session.SetString("UserEmail", (string)user.email);
        HttpContext.Session.SetString("UserId", (string)user.id);

        TempData["Success"] = "Login successful!";
        return RedirectToAction("Index", "Home");
    }

    // =========================
    // GET REGISTER
    // =========================
    [HttpGet]
    public IActionResult Register() => View();

    // =========================
    // POST REGISTER
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string FullName, string Email, string Password)
    {
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);

        var newUser = new
        {
            fullname = FullName,
            email = Email,
            password = hashedPassword
        };

        var json = JsonConvert.SerializeObject(newUser);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("social_sync_users", content);

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Failed to create account.";
            return View();
        }

        TempData["Success"] = "Account created successfully!";
        return RedirectToAction("Login");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        // Or use your authentication signout method
        return RedirectToAction("Login", "Auth");
    }
}
