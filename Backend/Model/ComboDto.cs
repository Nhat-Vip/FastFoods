using System.ComponentModel.DataAnnotations;

public class ComboDto
{
    [Required, MaxLength(100)]
    public string? Name { set; get; }
    [Required]
    public string? Description { set; get; }
    [Required]
    public decimal Price { set; get; }
    public IFormFile? ImageFile { set; get; }
    [Required]
    public string? ComboItems{ set; get; }
    
}