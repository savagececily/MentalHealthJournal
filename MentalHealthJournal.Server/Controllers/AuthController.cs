using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MentalHealthJournal.Models;
using MentalHealthJournal.Services;
using Microsoft.Extensions.Logging;

namespace MentalHealthJournal.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService userService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleTokenRequest request)
    {
        try
        {
            _logger.LogInformation("Google login attempt");
            // Get Google Client ID from configuration
            var googleClientId = _configuration["Google:ClientId"];
            if (string.IsNullOrEmpty(googleClientId))
            {
                _logger.LogError("Google Client ID not configured");
                return StatusCode(500, "Google authentication not configured");
            }

            // Validate the Google ID token
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            });

            if (payload == null)
            {
                _logger.LogWarning("Invalid Google token");
                return Unauthorized("Invalid Google token");
            }

            // Check if user exists
            var existingUser = await _userService.GetUserByProviderIdAsync(payload.Subject, "google");

            User user;
            if (existingUser != null)
            {
                // Update last login
                user = existingUser;
                user.LastLoginAt = DateTime.UtcNow;
                user = await _userService.CreateOrUpdateUserAsync(user);
            }
            else
            {
                // Create new user
                var newUserId = Guid.NewGuid().ToString();
                user = new User
                {
                    id = newUserId,
                    userId = newUserId,
                    Email = payload.Email,
                    Name = payload.Name,
                    ProfilePictureUrl = payload.Picture,
                    Provider = "google",
                    ProviderId = payload.Subject,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                user = await _userService.CreateOrUpdateUserAsync(user);
            }

            // Generate JWT token
            var jwtToken = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = jwtToken,
                User = user
            });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google token");
            return Unauthorized("Invalid Google token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google authentication");
            return StatusCode(500, "Authentication failed");
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<User>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var providerId = User.FindFirst("ProviderId")?.Value;
            var provider = User.FindFirst("Provider")?.Value;

            if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(provider))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByProviderIdAsync(providerId, provider);
            
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, "Failed to get user information");
        }
    }

    [HttpPut("username")]
    [Authorize]
    public async Task<ActionResult<User>> UpdateUsername([FromBody] UpdateUsernameRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest("Username cannot be empty");
            }

            if (request.Username.Length < 3 || request.Username.Length > 20)
            {
                return BadRequest("Username must be between 3 and 20 characters");
            }

            // Validate username format and length: 3-20 chars, alphanumeric and underscores only
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-z0-9_]{3,20}$"))
            {
                return BadRequest("Username can only contain lowercase letters, numbers, and underscores");
            }

            // Check if username is available
            var isAvailable = await _userService.IsUsernameAvailableAsync(request.Username, userIdClaim);
            if (!isAvailable)
            {
                return BadRequest("Username is already taken");
            }

            // Get current user
            var user = await _userService.GetUserByIdAsync(userIdClaim);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Update username
            user.Username = request.Username;
            user = await _userService.CreateOrUpdateUserAsync(user);

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating username");
            return StatusCode(500, "Failed to update username");
        }
    }

    [HttpGet("username/check")]
    [Authorize]
    public async Task<ActionResult<bool>> CheckUsernameAvailability([FromQuery] string username)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAvailable = await _userService.IsUsernameAvailableAsync(username, userIdClaim);
            return Ok(new { available = isAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username availability");
            return StatusCode(500, "Failed to check username availability");
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT Key not configured");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.userId), // Use userId (partition key) for consistency
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("ProviderId", user.ProviderId),
            new Claim("Provider", user.Provider)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
