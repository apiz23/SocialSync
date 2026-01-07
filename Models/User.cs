using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace SocialSync.Models
{
    public class User
    {
        [Key]
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("fullname")]
        public string? FullName { get; set; }

        [Required]
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("phone")]
        public string? Phone { get; set; }

        [JsonProperty("date_joined")]
        public DateTime DateJoined { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("bio")]
        public string? Bio { get; set; }

        [JsonProperty("persona1")]
        public string? Persona1 { get; set; }

        [JsonProperty("persona2")]
        public string? Persona2 { get; set; }

        [JsonProperty("persona3")]
        public string? Persona3 { get; set; }
    }
}