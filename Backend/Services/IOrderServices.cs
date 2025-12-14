using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

public interface IOrderServices
{
    Task<IEnumerable<Order>?> GetOrderByIdAsync(int orderId);
    Task<IEnumerable<Order>?> GetAllOrdersAsync();
    Task CreateOrderAsync(Order order);
    Task UpdateOrderAsync(Order order, OrderStatus orderStatus);
    // Task DeleteOrderAsync(Order order);
}
public class OrderServices : IOrderServices
{
    private readonly AppDbContext _context;
    private readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();

    public OrderServices(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Order>?> GetOrderByIdAsync(int orderId)
    {
        return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.FastFood)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Combo)
                        .ThenInclude(c => c.ComboItems)
                            .ThenInclude(ci => ci.FastFood)
                .Where(o => o.UserId == orderId).ToListAsync();
    }

    public async Task<IEnumerable<Order>?> GetAllOrdersAsync()
    {
        var today = DateTime.Today;            // 00:00 hôm nay
        var yesterday = DateTime.Today.AddDays(-1); // 00:00 hôm qua

        var orders = await _context.Orders
        .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.FastFood)
        .Include(o => o.User)
        .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Combo)
                .ThenInclude(c => c.ComboItems)
                    .ThenInclude(ci => ci.FastFood)
        .Where(o => o.OrderDate >= yesterday && o.OrderDate < today.AddDays(1))
        .ToListAsync();
        return orders;
    }

    public async Task CreateOrderAsync(Order order)
    {
        order.Notes = _sanitizer.Sanitize(order.Notes ?? string.Empty);
        order.CustomerName = _sanitizer.Sanitize(order.CustomerName ?? string.Empty);
        order.Address = _sanitizer.Sanitize(order.Address);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateOrderAsync(Order order, OrderStatus orderStatus)
    {

        order.Status = orderStatus;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }

    // public async Task DeleteOrderAsync(int orderId)
    // {
    //     var order = await _context.Orders.FindAsync(orderId);
    //     if (order != null)
    //     {
    //         _context.Orders.Remove(order);
    //         await _context.SaveChangesAsync();
    //     }
    // }
}