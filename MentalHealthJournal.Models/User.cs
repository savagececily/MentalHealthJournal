namespace MentalHealthJournal.Models;

public class User
{
    public string id { get; set; } = string.Empty; // Cosmos DB document ID
    public string userId { get; set; } = string.Empty; // Partition key
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Username { get; set; } // Custom username set by user
    public string? ProfilePictureUrl { get; set; }
    public string Provider { get; set; } = "google"; // google, facebook, microsoft, etc.
    public string ProviderId { get; set; } = string.Empty; // The ID from the provider
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}
