using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SocialSync.Models
{
    public class Event
    {
        [JsonProperty("id")]
        public long EventId { get; set; }

        [Required(ErrorMessage = "Event title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        [JsonProperty("title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        [JsonProperty("event_date")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(255, ErrorMessage = "Location cannot exceed 255 characters")]
        [JsonProperty("location")]
        public string Location { get; set; }

        [Range(1, 1000, ErrorMessage = "Max participants must be between 1 and 1000")]
        [JsonProperty("max_participants")]
        public int? MaxParticipants { get; set; }

        [JsonProperty("created_by")]
        public string CreatedBy { get; set; }

        [JsonProperty("creator_id")]
        public Guid? CreatorId { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "Upcoming";

        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        [JsonProperty("image_url")]
        public string? ImageUrl { get; set; }

        // Participant count (calculated)
        [JsonProperty("participant_count")]
        public int ParticipantCount { get; set; } = 0;

        // Check if current user has joined
        public bool HasJoined { get; set; } = false;

        // Check if event is full
        public bool IsFull => MaxParticipants.HasValue && ParticipantCount >= MaxParticipants.Value;

        // Check if event has passed
        public bool HasPassed => EventDate < DateTime.UtcNow;
    }

    public class EventParticipant
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("event_id")]
        public long EventId { get; set; }

        [JsonProperty("user_email")]
        public string UserEmail { get; set; }

        [JsonProperty("user_id")]
        public Guid UserId { get; set; }

        [JsonProperty("joined_at")]
        public DateTime JoinedAt { get; set; }
    }
}