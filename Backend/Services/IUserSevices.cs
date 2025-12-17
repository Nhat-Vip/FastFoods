using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public interface IUserServices
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<IEnumerable<User>?> GetAllUsersAsync();
    Task CreateUserAsync(User user);
    Task UpdateUserAsync(User currentUser,User updatedUser);
    Task DeleteUserAsync(User user);
    Task LockUserAsync(User user);
}
public class UserServices : IUserServices
{
    private readonly AppDbContext _context;
    private readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();

    public UserServices(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<IEnumerable<User>?> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task CreateUserAsync(User newUser)
    {
        var hasher = new PasswordHasher<User>();
        newUser.PasswordHash = hasher.HashPassword(newUser, newUser.PasswordHash);
        newUser.Address = _sanitizer.Sanitize(newUser.Address);
        newUser.FullName = _sanitizer.Sanitize(newUser.FullName);
        
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User currentUser,User updateUser)
    {
        currentUser.FullName = _sanitizer.Sanitize(updateUser.FullName);
        currentUser.Address = _sanitizer.Sanitize(updateUser.Address);
        currentUser.Phone = updateUser.Phone;
        currentUser.DateOfBirth = updateUser.DateOfBirth;
        currentUser.UserRole = updateUser.UserRole;
        currentUser.Username = updateUser.Username;
        currentUser.Email = updateUser.Email;
        // _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(User user)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u=>u.Email == email);

    }

    public async Task LockUserAsync(User user)
    {
        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
    }
}