    using System.ComponentModel.DataAnnotations;

    public class Category
    {
        public int CategoryId { get; set; }

        [Required, MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }

        // public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<FastFood> FastFoods { get; set; } = new List<FastFood>();
    }