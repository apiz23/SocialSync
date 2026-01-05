namespace SocialSync.Models
{
    public class ErrorViewModel
    {
        // Request ID for tracking errors
        public string? RequestId { get; set; }

        // Whether to show the RequestId in the UI
        public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
    }
}
