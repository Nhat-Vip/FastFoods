using AngleSharp.Browser;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

public interface IFastFoodServices
{
    Task<FastFood?> GetFastFoodByIdAsync(int fastFoodId);
    Task<IEnumerable<FastFood>?> GetAllFastFoodsAsync();
    Task<IEnumerable<FastFood>?> GetAllFastFoodsByAdminAsync();
    Task<FastFood> CreateFastFoodAsync(FastFoodDto dto,HttpContext context);
    Task<FastFood> UpdateFastFoodAsync(FastFood fastFood,FastFoodDto dto, HttpContext context);
    Task DeleteFastFoodAsync(FastFood fastFood);
    Task LockFastFoodAsync(FastFood fastFood);
}
public class FastFoodServices : IFastFoodServices
{
    private readonly AppDbContext _context;
    private readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();
    public FastFoodServices(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FastFood?> GetFastFoodByIdAsync(int fastFoodId)
    {
        return await _context.FastFoods
                    .Include(ff=>ff.Category)
                    .FirstOrDefaultAsync(ff=>ff.FastFoodId == fastFoodId);
    }

    public async Task<IEnumerable<FastFood>?> GetAllFastFoodsAsync()
    {
        return await _context.FastFoods
        .Include(ff => ff.Category)
        .Where(ff => ff.IsActive)
        .ToListAsync();
    }

    public async Task<FastFood> CreateFastFoodAsync(FastFoodDto dto, HttpContext context)
    {
        // Tạo thư mục nếu chưa có
        var folder = Path.Combine("wwwroot", "images", "foods");
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(dto.ImageFile.FileName).ToLowerInvariant();
        // Tên file unique
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folder, fileName);

        // Lưu file
        await using var stream = new FileStream(filePath, FileMode.Create);
        await dto.ImageFile.CopyToAsync(stream);

        // Trả về URL để frontend dùng
        var request = context.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var imageUrl = $"{baseUrl}/images/foods/{fileName}";

        var food = new FastFood
        {
            Name = _sanitizer.Sanitize(dto.Name),
            Description = _sanitizer.Sanitize(dto.Description),
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            ImageUrl = imageUrl
        };


        _context.FastFoods.Add(food);
        await _context.SaveChangesAsync();
        return food;
    }

    public async Task<FastFood> UpdateFastFoodAsync(FastFood fastFood,FastFoodDto dto, HttpContext context)
    {
        if (dto.ImageFile != null && dto.ImageFile.Length > 0)
        {
            var folder = Path.Combine("wwwroot", "images", "foods");
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(dto.ImageFile.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await dto.ImageFile.CopyToAsync(stream);

            var request = context.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var imageUrl = $"{baseUrl}/images/foods/{fileName}";
            fastFood.ImageUrl = imageUrl;
        }

        fastFood.Name = dto.Name!;
        fastFood.Description = dto.Description!;
        fastFood.Price = dto.Price;
        fastFood.CategoryId = dto.CategoryId;

        await _context.SaveChangesAsync();
        return fastFood;
    }

    public async Task DeleteFastFoodAsync(FastFood fastFood)
    {
        _context.FastFoods.Remove(fastFood);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<FastFood>?> GetAllFastFoodsByAdminAsync()
    {
        return await _context.FastFoods
        .Include(ff => ff.Category)
        .ToListAsync();
    }

    public async Task LockFastFoodAsync(FastFood fastFood)
    {
        fastFood.IsActive = !fastFood.IsActive;
        await _context.SaveChangesAsync();
    }
}