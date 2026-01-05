using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SocialSync.Models;
using System.Net.Http.Headers;
using System.Text;

namespace SocialSync.Controllers
{
    public class EventsController : Controller
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;

        public EventsController(IHttpClientFactory clientFactory, IConfiguration config)
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
        }

        // ============================
        // GET: EVENTS LIST
        // ============================
        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _http.GetAsync(
                    "social_sync_events?select=*&order=event_date.asc"
                );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to load events.";
                    return View(new List<Event>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var events = JsonConvert.DeserializeObject<List<Event>>(json) ?? new List<Event>();

                // Get current user
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");

                // Get participant counts and check if user joined
                foreach (var evt in events)
                {
                    // Get participant count
                    var countResponse = await _http.GetAsync(
                        $"social_sync_event_participants?event_id=eq.{evt.EventId}&select=id"
                    );
                    if (countResponse.IsSuccessStatusCode)
                    {
                        var countJson = await countResponse.Content.ReadAsStringAsync();
                        var participants = JsonConvert.DeserializeObject<List<EventParticipant>>(countJson);
                        evt.ParticipantCount = participants?.Count ?? 0;
                    }

                    // Check if current user joined
                    if (!string.IsNullOrEmpty(currentUserEmail))
                    {
                        var joinResponse = await _http.GetAsync(
                            $"social_sync_event_participants?event_id=eq.{evt.EventId}&user_email=eq.{currentUserEmail}"
                        );
                        if (joinResponse.IsSuccessStatusCode)
                        {
                            var joinJson = await joinResponse.Content.ReadAsStringAsync();
                            var userParticipants = JsonConvert.DeserializeObject<List<EventParticipant>>(joinJson);
                            evt.HasJoined = userParticipants?.Any() ?? false;
                        }
                    }

                    // Update status based on date
                    evt.Status = evt.EventDate < DateTime.UtcNow ? "Completed" : "Upcoming";
                }

                return View(events);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(new List<Event>());
            }
        }

        // ============================
        // GET: CREATE EVENT
        // ============================
        public IActionResult Create()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "Please login to create an event.";
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // ============================
        // POST: CREATE EVENT
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model)
        {
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatorId");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate event date is in the future
            if (model.EventDate <= DateTime.UtcNow)
            {
                ModelState.AddModelError("EventDate", "Event date must be in the future.");
                return View(model);
            }

            var creatorEmail = HttpContext.Session.GetString("UserEmail");
            var creatorIdRaw = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(creatorEmail) || string.IsNullOrEmpty(creatorIdRaw))
            {
                TempData["Error"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Auth");
            }

            if (!Guid.TryParse(creatorIdRaw, out var creatorId))
            {
                TempData["Error"] = "Invalid user session.";
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var eventData = new
                {
                    title = model.Title?.Trim(),
                    description = model.Description?.Trim(),
                    event_date = model.EventDate.ToUniversalTime(),
                    location = model.Location?.Trim(),
                    max_participants = model.MaxParticipants,
                    created_by = creatorEmail,
                    creator_id = creatorId,
                    status = "Upcoming",
                    image_url = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim()
                };

                var json = JsonConvert.SerializeObject(eventData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("social_sync_events", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Failed to create event: {errorContent}";
                    return View(model);
                }

                TempData["Success"] = "Event created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return View(model);
            }
        }

        // ============================
        // GET: EDIT EVENT
        // ============================
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var response = await _http.GetAsync($"social_sync_events?id=eq.{id}&select=*");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Event not found.";
                    return RedirectToAction("Index");
                }

                var json = await response.Content.ReadAsStringAsync();
                var events = JsonConvert.DeserializeObject<List<Event>>(json);

                if (events == null || !events.Any())
                {
                    TempData["Error"] = "Event not found.";
                    return RedirectToAction("Index");
                }

                var evt = events[0];
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");

                if (evt.CreatedBy != currentUserEmail)
                {
                    TempData["Error"] = "You don't have permission to edit this event.";
                    return RedirectToAction("Index");
                }

                return View(evt);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ============================
        // POST: EDIT EVENT
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Edit")]
        public async Task<IActionResult> EditEvent(Event model)
        {
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatorId");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");
                var checkResponse = await _http.GetAsync($"social_sync_events?id=eq.{model.EventId}&select=created_by");
                var checkJson = await checkResponse.Content.ReadAsStringAsync();
                var events = JsonConvert.DeserializeObject<List<Event>>(checkJson);

                if (events == null || !events.Any() || events[0].CreatedBy != currentUserEmail)
                {
                    TempData["Error"] = "You don't have permission to edit this event.";
                    return RedirectToAction("Index");
                }

                var payload = new
                {
                    title = model.Title?.Trim(),
                    description = model.Description?.Trim(),
                    event_date = model.EventDate.ToUniversalTime(),
                    location = model.Location?.Trim(),
                    max_participants = model.MaxParticipants,
                    image_url = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim()
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PatchAsync($"social_sync_events?id=eq.{model.EventId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to update event.";
                    return View(model);
                }

                TempData["Success"] = "Event updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }

        // ============================
        // GET: DELETE EVENT
        // ============================
        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var response = await _http.GetAsync($"social_sync_events?id=eq.{id}&select=*");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Event not found.";
                    return RedirectToAction("Index");
                }

                var json = await response.Content.ReadAsStringAsync();
                var events = JsonConvert.DeserializeObject<List<Event>>(json);

                if (events == null || !events.Any())
                {
                    TempData["Error"] = "Event not found.";
                    return RedirectToAction("Index");
                }

                var evt = events[0];
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");

                if (evt.CreatedBy != currentUserEmail)
                {
                    TempData["Error"] = "You don't have permission to delete this event.";
                    return RedirectToAction("Index");
                }

                return View(evt);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ============================
        // POST: DELETE EVENT
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(long Id)
        {
            try
            {
                var currentUserEmail = HttpContext.Session.GetString("UserEmail");

                var checkResponse = await _http.GetAsync(
                    $"social_sync_events?id=eq.{Id}&select=created_by"
                );

                var checkJson = await checkResponse.Content.ReadAsStringAsync();
                var events = JsonConvert.DeserializeObject<List<Event>>(checkJson);

                if (events == null || !events.Any() || events[0].CreatedBy != currentUserEmail)
                {
                    TempData["Error"] = "You don't have permission to delete this event.";
                    return RedirectToAction("Index");
                }

                await _http.DeleteAsync(
                    $"social_sync_event_participants?event_id=eq.{Id}"
                );

                var response = await _http.DeleteAsync(
                    $"social_sync_events?id=eq.{Id}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to delete event.";
                    return RedirectToAction("Delete", new { id = Id });
                }

                TempData["Success"] = "Event deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }



        // ============================
        // POST: JOIN EVENT
        // ============================
        [HttpPost]
        public async Task<IActionResult> JoinEvent(long id)
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("UserEmail");
                var userIdRaw = HttpContext.Session.GetString("UserId");

                if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(userIdRaw))
                {
                    return Json(new { success = false, message = "Please login to join events." });
                }

                if (!Guid.TryParse(userIdRaw, out var userId))
                {
                    return Json(new { success = false, message = "Invalid session." });
                }

                // Check if already joined
                var checkResponse = await _http.GetAsync(
                    $"social_sync_event_participants?event_id=eq.{id}&user_id=eq.{userId}"
                );
                var checkJson = await checkResponse.Content.ReadAsStringAsync();
                var existing = JsonConvert.DeserializeObject<List<EventParticipant>>(checkJson);

                if (existing?.Any() == true)
                {
                    return Json(new { success = false, message = "You already joined this event." });
                }

                // Check if event is full
                var eventResponse = await _http.GetAsync($"social_sync_events?id=eq.{id}&select=max_participants");
                var eventJson = await eventResponse.Content.ReadAsStringAsync();
                var events = JsonConvert.DeserializeObject<List<Event>>(eventJson);

                if (events?.Any() == true && events[0].MaxParticipants.HasValue)
                {
                    var countResponse = await _http.GetAsync($"social_sync_event_participants?event_id=eq.{id}&select=id");
                    var countJson = await countResponse.Content.ReadAsStringAsync();
                    var participants = JsonConvert.DeserializeObject<List<EventParticipant>>(countJson);

                    if (participants?.Count >= events[0].MaxParticipants.Value)
                    {
                        return Json(new { success = false, message = "Event is full." });
                    }
                }

                // Join event
                var participantData = new
                {
                    event_id = id,
                    user_email = userEmail,
                    user_id = userId
                };

                var json = JsonConvert.SerializeObject(participantData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("social_sync_event_participants", content);

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Failed to join event." });
                }

                return Json(new { success = true, message = "Successfully joined event!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================
        // POST: LEAVE EVENT
        // ============================
        [HttpPost]
        public async Task<IActionResult> LeaveEvent(long id)
        {
            try
            {
                var userIdRaw = HttpContext.Session.GetString("UserId");

                if (string.IsNullOrEmpty(userIdRaw))
                {
                    return Json(new { success = false, message = "Please login." });
                }

                if (!Guid.TryParse(userIdRaw, out var userId))
                {
                    return Json(new { success = false, message = "Invalid session." });
                }

                var response = await _http.DeleteAsync(
                    $"social_sync_event_participants?event_id=eq.{id}&user_id=eq.{userId}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Failed to leave event." });
                }

                return Json(new { success = true, message = "Successfully left event!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}