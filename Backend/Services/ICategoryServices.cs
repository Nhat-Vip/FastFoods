using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

public interface ICategoryServices
{
    Task<Category?> GetCategoryByIdAsync(int categoryId);
    Task<IEnumerable<Category>?> GetAllCategoriesAsync();
    Task<Category> CreateCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category,Category updateCategory);
    Task DeleteCategoryAsync(Category category);
}
public class CategoryServices : ICategoryServices
{
    private readonly AppDbContext _context;
    private readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();

    public CategoryServices(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetCategoryByIdAsync(int categoryId)
    {
        return await _context.Categories
                .Include(c => c.FastFoods)
                .FirstOrDefaultAsync(c=>c.CategoryId == categoryId);
    }

    public async Task<IEnumerable<Category>?> GetAllCategoriesAsync()
    {
        return await _context.Categories.Include(c => c.FastFoods).ToListAsync();
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        category.CategoryName = _sanitizer.Sanitize(category.CategoryName);
        category.Description = _sanitizer.Sanitize(category.Description ?? string.Empty);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task UpdateCategoryAsync(Category category,Category updateCategory)
    {
        category.CategoryName = _sanitizer.Sanitize(updateCategory.CategoryName);
        category.Description = _sanitizer.Sanitize(updateCategory.Description ?? string.Empty);
        // _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(Category category)
    {
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}