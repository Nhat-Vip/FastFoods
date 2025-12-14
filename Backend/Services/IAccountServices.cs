using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public interface IAccountServices
{
    Task<string?> LoginAsync(LoginRequest loginRequest,User user);
    Task<User?> GetCurrentUserAsync(string? email);
    Task RegisterAsync(User user);
}
public class AccountServices : IAccountServices
{
    private readonly AppDbContext _context;
    private readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();
    private readonly IConfiguration _configuration;

    public AccountServices(AppDbContext context, IConfiguration configuration)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<User?> GetCurrentUserAsync(string? email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<string?> LoginAsync(LoginRequest login, User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new Claim("Id", user.UserId.ToString()),
            new Claim("UserName", user.Username),
            new Claim("Email", user.Email),
            new Claim(ClaimTypes.Role, user.UserRole.ToString())

        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: signIn
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task RegisterAsync(User user)
    {
        user.Address = _sanitizer.Sanitize(user.Address);
        user.FullName = _sanitizer.Sanitize(user.FullName);
        user.Username = _sanitizer.Sanitize(user.Username);
        user.UserRole = UserRole.Customer;
        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
}