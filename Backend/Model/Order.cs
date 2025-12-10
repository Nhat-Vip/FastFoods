using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }
    [Required]
    public string? CustomerName{ set; get; }

    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    public bool IsPaid { get; set; } = false;

    public string? Notes { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
public enum OrderStatus
{
    Pending = 0,
    Preparing = 1,
    Delivering = 2,
    Completed = 3,
    Cancelled = 4
}
public class OrderStatusDto
{
    public string Status { set; get; } = string.Empty;
};