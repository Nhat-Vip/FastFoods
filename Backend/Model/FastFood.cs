using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class FastFood
{
    public int FastFoodId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty; // Thông tin chi tiết

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public int? CategoryId { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public virtual Category? Category { get; set; }
    public virtual ICollection<ComboItem>? ComboItems { get; set; } = new List<ComboItem>();
    public virtual ICollection<OrderItem>? OrderItems { get; set; } = new List<OrderItem>();
}