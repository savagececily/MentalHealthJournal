namespace MentalHealthJournal.Models;

public class GoogleTokenRequest
{
    public string IdToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}
