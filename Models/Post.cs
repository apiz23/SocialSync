using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SocialSync.Models
{
    public class Post
    {
        [JsonProperty("id")]
        public long PostId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [JsonProperty("title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required")]
        [StringLength(5000, ErrorMessage = "Content cannot exceed 5000 characters")]
        [JsonProperty("content")]
        public string Content { get; set; }

        // 🔑 Stored in posts table (EMAIL)
        [JsonProperty("author")]
        public string Author { get; set; }

        // ✅ ADD THIS - Author ID for foreign key
        [JsonProperty("author_id")]
        public Guid? AuthorId { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; } = "General";

        [JsonProperty("created_at")]
        public DateTime CreatedDate { get; set; }

        // ✅ OPTIONAL IMAGE
        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        [JsonProperty("image_url")]
        public string? ImageUrl { get; set; }

        [JsonProperty("likes")]
        public int Likes { get; set; } = 0;

        [JsonProperty("comments")]
        public int Comments { get; set; } = 0;

        // 🔗 JOINED USER DATA FROM sosial_sync_users
        [JsonProperty("user")]
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        [JsonProperty("fullname")]
        public string? FullName { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }
    }
}