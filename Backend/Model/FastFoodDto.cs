using System.ComponentModel.DataAnnotations;

public class FastFoodDto
{
    [Required, MaxLength(100)]
    public string? Name { get; set; }
    [Required]
    public string? Description { get; set; }
    [Required]
    public decimal Price { get; set; }
    [Required]
    public int? CategoryId { get; set; }
    public IFormFile? ImageFile { get; set; }
}
