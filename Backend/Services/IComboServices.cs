using System.Text.Json;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

public interface IComboServices
{
    Task<Combo?> GetComboByIdAsync(int comboId);
    Task<IEnumerable<Combo>?> GetAllCombosAsync();
    Task<IEnumerable<Combo>?> GetAllCombosByAdminAsync();
    Task<Combo?> CreateComboAsync(ComboDto combo,HttpContext context);
    Task<Combo?> UpdateComboAsync(Combo combo, ComboDto dto, HttpContext context);
    Task DeleteComboAsync(Combo combo);
    Task LockComboAsync(Combo combo);
}
public class ComboServices : IComboServices
{
    private readonly AppDbContext _context;
    private readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();

    public ComboServices(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Combo?> GetComboByIdAsync(int comboId)
    {
        return await _context.Combos
            .Include(c => c.ComboItems)
            .ThenInclude(ci => ci.FastFood)
            .FirstOrDefaultAsync(c => c.ComboId == comboId);
    }

    public async Task<IEnumerable<Combo>?> GetAllCombosAsync()
    {
        return await _context.Combos
        .Include(c => c.ComboItems)
            .ThenInclude(ci => ci.FastFood)
        .Where(c => c.IsActive)
        .ToListAsync();
    }

    public async Task<Combo?> CreateComboAsync(ComboDto dto, HttpContext context)
    {
        // Tạo thư mục nếu chưa có
        var folder = Path.Combine("wwwroot", "images", "combos");
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
        var imageUrl = $"{baseUrl}/images/combos/{fileName}";
        var items = JsonSerializer.Deserialize<List<ComboItem>>(dto.ComboItems);
        var combo = new Combo
        {
            Name = dto.Name!,
            Description = dto.Description!,
            Price = dto.Price,
            ImageUrl = imageUrl,
            ComboItems = items,
        };
        _context.Combos.Add(combo);
        await _context.SaveChangesAsync();
        return await GetComboByIdAsync(combo.ComboId);
    }

    public async Task<Combo?> UpdateComboAsync(Combo combo,ComboDto dto, HttpContext context)
    {
        if (dto.ImageFile != null && dto.ImageFile.Length > 0)
        {
            var folder = Path.Combine("wwwroot", "images", "combos");
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(dto.ImageFile.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await dto.ImageFile.CopyToAsync(stream);

            var request = context.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var imageUrl = $"{baseUrl}/images/combos/{fileName}";
            combo.ImageUrl = imageUrl;
        }
        _context.ComboItems.RemoveRange(combo.ComboItems);
        combo.Description = dto.Description!;
        combo.Name = dto.Name!;
        combo.Price = dto.Price;
        combo.ComboItems = JsonSerializer.Deserialize<List<ComboItem>>(dto.ComboItems!)!;

        await _context.SaveChangesAsync();
        return await GetComboByIdAsync(combo.ComboId);
    }

    public async Task DeleteComboAsync(Combo combo)
    {
        _context.Combos.Remove(combo);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Combo>?> GetAllCombosByAdminAsync()
    {
        return await _context.Combos
        .Include(c => c.ComboItems)
            .ThenInclude(ci => ci.FastFood)
        .ToListAsync();
    }

    public Task LockComboAsync(Combo combo)
    {
        combo.IsActive = !combo.IsActive;
        return _context.SaveChangesAsync();
    }
}