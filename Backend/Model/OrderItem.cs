using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    [Required]
    public OrderItemType ItemType { get; set; }

    public int? FastFoodId { get; set; }
    public int? ComboId { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; } // Giá tại thời điểm đặt
    public string? ComboName { set; get; } // Tên Combo lúc đặt nếu có
    public string? FastFoodName{ set; get; } // Tên FastFood lúc đặt nếu có

    // Navigation
    public Order Order { get; set; } = null!;
    public FastFood? FastFood { get; set; }
    public Combo? Combo { get; set; }
}
public enum OrderItemType
{
    FastFood = 0,
    Combo = 1
}