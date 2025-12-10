using System.ComponentModel.DataAnnotations;

public class User
{
    public int UserId { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(100), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty; // BCrypt hash

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty; // Bắt buộc Guest

    [Required, MaxLength(11), Phone]
    public string Phone { get; set; } = string.Empty; // Bắt buộc Guest

    [Required]
    public string Address { get; set; } = string.Empty; // Bắt buộc Guest

    public DateTime? DateOfBirth { get; set; }

    [Required]
    public UserRole UserRole { get; set; } = UserRole.Customer;

    [MaxLength(100)]
    public string? GoogleId { get; set; } // Đăng nhập Google

    public bool IsActive { get; set; } = true; // Soft delete

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation Properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
public enum UserRole
{
    Customer = 1,
    Admin = 2
}