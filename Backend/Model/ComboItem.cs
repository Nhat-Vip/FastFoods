public class ComboItem
{
    public int ComboItemId { get; set; }

    public int ComboId { get; set; }
    public int FastFoodId { get; set; }
    public int Quantity { get; set; } = 1;

    // Navigation
    public virtual Combo Combo { get; set; } = null!;
    public virtual FastFood FastFood { get; set; } = null!;
}
public class ComboItemDto
{
    public int FastFoodId { get; set; }
    public int Quantity { get; set; }
}
