using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<FastFood> FastFoods { get; set; }
    public DbSet<Combo> Combos { get; set; }
    public DbSet<ComboItem> ComboItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Relationships
        modelBuilder.Entity<ComboItem>()
            .HasKey(ci => ci.ComboItemId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.FastFood)
            .WithMany(ff => ff.OrderItems)
            .HasForeignKey(oi => oi.FastFoodId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Combo)
            .WithMany(c => c.OrderItems)
            .HasForeignKey(oi => oi.ComboId)
            .OnDelete(DeleteBehavior.Restrict);

    //     DateTime SeedDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);
    //     modelBuilder.Entity<User>().HasData(
    //     new User
    //     {
    //         Username = "admin",
    //         Email = "admin@gmail.com",
    //         PasswordHash = "123", // đã hash
    //         FullName = "Quản trị viên",
    //         Phone = "0901234567",
    //         Address = "123 Đường Admin, TP.HCM",
    //         UserType = UserType.Admin,
    //         IsActive = true,
    //         CreatedAt = SeedDate
    //     },
    //     new User
    //     {
    //         Username = "khachhang1",
    //         Email = "khach1@gmail.com",
    //         PasswordHash = "123",
    //         FullName = "Nguyễn Văn A",
    //         Phone = "0909876543",
    //         Address = "Quận 1, TP.HCM",
    //         UserType = UserType.Customer,
    //         IsActive = true,
    //         CreatedAt = SeedDate
    //     },
    //     new User
    //     {
    //         Username = "khachhang2",
    //         Email = "khach2@gmail.com",
    //         PasswordHash ="123",
    //         FullName = "Trần Thị B",
    //         Phone = "0912345678",
    //         Address = "Quận 7, TP.HCM",
    //         UserType = UserType.Customer,
    //         GoogleId = "google_123456789", // đăng nhập Google
    //         IsActive = true,
    //         CreatedAt = SeedDate
    //     }
    // );

    //     // === 2. Seed Categories ===
    //     modelBuilder.Entity<Category>().HasData(
    //         new Category { CategoryId = 1, CategoryName = "Burger", Description = "Burger bò, gà, cá" },
    //         new Category { CategoryId = 2, CategoryName = "Gà rán", Description = "Gà rán giòn tan" },
    //         new Category { CategoryId = 3, CategoryName = "Cơm tấm", Description = "Cơm tấm sườn bì chả" },
    //         new Category { CategoryId = 4, CategoryName = "Trà sữa", Description = "Trà sữa trân châu đường đen" },
    //         new Category { CategoryId = 5, CategoryName = "Combo gia đình", Description = "Combo cho 4-6 người" }
    //     );

    //     // === 3. Seed 10 món ăn (FastFood) ===
    //     modelBuilder.Entity<FastFood>().HasData(
    //         new FastFood { FastFoodId = 1, Name = "Burger Bò Nướng", Description = "Burger bò nướng than, phô mai tan chảy", Price = 69000, CategoryId = 1, ImageUrl = "/images/burger-bo.jpg", IsActive = true },
    //         new FastFood { FastFoodId = 2, Name = "Burger Gà Quay", Description = "Gà quay nguyên con, sốt BBQ", Price = 79000, CategoryId = 1, ImageUrl = "/images/burger-ga.jpg", IsActive = true },
    //         new FastFood { FastFoodId = 3, Name = "Gà Rán 6 Miếng", Description = "6 miếng gà rán cay/ngọt", Price = 129000, CategoryId = 2, ImageUrl = "/images/ga-ran.jpg", IsActive = true },
    //         new FastFood { FastFoodId = 4, Name = "Cơm Tấm Sườn Nướng", Description = "Sườn nướng mật ong + bì + chả", Price = 59000, CategoryId = 3, ImageUrl = "/images/com-tam.jpg", IsActive = true },
    //         new FastFood { FastFoodId = 5, Name = "Trà Sữa Trân Châu", Description = "Trân châu đường đen, sữa tươi", Price = 35000, CategoryId = 4, ImageUrl = "/images/trasua.jpg", IsActive = true },
    //         new FastFood { FastFoodId = 6, Name = "Mì Ý Sốt Bò Bằm", Description = "Mì Ý sốt bò bằm kiểu Ý", Price = 65000, CategoryId = null, ImageUrl = "/images/mi-y.jpg", IsActive = true },
    //         new FastFood { FastFoodId = 7, Name = "Khoai Tây Chiên Lớn", Description = "Khoai tây chiên giòn tan", Price = 29000, CategoryId = null, ImageUrl = "/images/khoai-tay.jpg", IsActive = true },
    //         new FastFood { FastFoodId = 8, Name = "Salad Rau Xanh", Description = "Salad tươi + sốt mè rang", Price = 45000, CategoryId = null, ImageUrl = "/images/salad.jpg", IsActive = true },
    //         new FastFood { FastFoodId = 9, Name = "Pizza Hải Sản", Description = "Pizza hải sản size M", Price = 159000, CategoryId = null, ImageUrl = "/images/pizza.jpg", IsActive = true },
    //         new FastFood { FastFoodId = 10, Name = "Nước Ngọt Coca", Description = "Coca Cola 330ml", Price = 15000, CategoryId = null, ImageUrl = "/images/coca.jpg", IsActive = true }
    //     );

    //     // === 4. Seed 10 Combo (rất thực tế) ===
    //     modelBuilder.Entity<Combo>().HasData(
    //         new Combo { ComboId = 1, Name = "Combo Burger Đôi", Description = "2 Burger Bò + 1 Khoai Tây + 2 Nước", Price = 179000, ImageUrl = "/images/combo1.jpg" },
    //         new Combo { ComboId = 2, Name = "Combo Gà Rán Gia Đình", Description = "8 miếng gà + 2 khoai + 4 nước", Price = 299000, ImageUrl = "/images/combo-ga.jpg" },
    //         new Combo { ComboId = 3, Name = "Combo Cơm Tấm Đôi", Description = "2 phần cơm tấm + 2 trà sữa", Price = 139000, ImageUrl = "/images/combo-comtam.jpg" },
    //         new Combo { ComboId = 4, Name = "Combo Trà Sữa 4 Người", Description = "4 trà sữa + topping full", Price = 129000, ImageUrl = "/images/combo-trasua.jpg" },
    //         new Combo { ComboId = 5, Name = "Combo Pizza Party", Description = "1 Pizza + 1 Gà + 4 Nước", Price = 399000, ImageUrl = "/images/combo-pizza.jpg" },
    //         new Combo { ComboId = 6, Name = "Combo Ăn Sáng", Description = "Burger + Trà sữa + Khoai tây", Price = 99000, ImageUrl = "/images/combo-sang.jpg" },
    //         new Combo { ComboId = 7, Name = "Combo Siêu Tiết Kiệm", Description = "Burger + Khoai + Nước", Price = 99000, ImageUrl = "/images/combo-tietkiem.jpg" },
    //         new Combo { ComboId = 8, Name = "Combo Healthy", Description = "Salad + Nước ép + Trà sữa ít đường", Price = 119000, ImageUrl = "/images/combo-healthy.jpg" },
    //         new Combo { ComboId = 9, Name = "Combo Sinh Nhật", Description = "Pizza + Gà 6 + 6 Nước", Price = 499000, ImageUrl = "/images/combo-sinhnhat.jpg" },
    //         new Combo { ComboId = 10, Name = "Combo Chill Phố", Description = "Mì Ý + Trà sữa + Khoai tây", Price = 129000, ImageUrl = "/images/combo-chill.jpg" }
    //     );

    //     // === 5. Seed ComboItems (chi tiết combo gồm món nào) ===
    //     modelBuilder.Entity<ComboItem>().HasData(
    //         // Combo 1: Burger Đôi
    //         new ComboItem { ComboItemId = 1, ComboId = 1, FastFoodId = 1, Quantity = 2 },
    //         new ComboItem { ComboItemId = 2, ComboId = 1, FastFoodId = 7, Quantity = 1 },
    //         new ComboItem { ComboItemId = 3, ComboId = 1, FastFoodId = 10, Quantity = 2 },

    //         // Combo 2: Gà Gia Đình
    //         new ComboItem { ComboItemId = 4, ComboId = 2, FastFoodId = 3, Quantity = 1 }, // 6 miếng → dùng món 3
    //         new ComboItem { ComboItemId = 5, ComboId = 2, FastFoodId = 7, Quantity = 2 },
    //         new ComboItem { ComboItemId = 6, ComboId = 2, FastFoodId = 10, Quantity = 4 },

    //         // Combo 3: Cơm Tấm Đôi
    //         new ComboItem { ComboItemId = 7, ComboId = 3, FastFoodId = 4, Quantity = 2 },
    //         new ComboItem { ComboItemId = 8, ComboId = 3, FastFoodId = 5, Quantity = 2 },

    //         // Combo 6: Ăn Sáng
    //         new ComboItem { ComboItemId = 9, ComboId = 6, FastFoodId = 1, Quantity = 1 },
    //         new ComboItem { ComboItemId = 10, ComboId = 6, FastFoodId = 5, Quantity = 1 },
    //         new ComboItem { ComboItemId = 11, ComboId = 6, FastFoodId = 7, Quantity = 1 }
    //     );
    }
}   