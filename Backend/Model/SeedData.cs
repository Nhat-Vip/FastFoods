using Microsoft.AspNetCore.Identity;

public static class SeedData
{
    public static async Task Initialize(AppDbContext db)
    {
        if (!db.Users.Any())
        {
            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Username = "ADMIN",
                Email = "Admin@gmail.com",
                FullName = "Nguyễn ADMIN",
                Phone = "0987654321",
                Address = "ABC",
                UserRole = UserRole.Admin,
            };
            user.PasswordHash = hasher.HashPassword(user, "123");
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        if (!db.Categories.Any() || !db.FastFoods.Any())
        {
            var categories = new List<Category>
            {
                new Category
                {
                    CategoryName = "Hamburger",
                    Description = "Các loại hamburger"
                },
                new Category
                {
                    CategoryName = "Pizza",
                    Description = "Pizza các loại"
                },
                new Category
                {
                    CategoryName = "Drink",
                    Description = "Đồ uống giải khát"
                }
            };

            db.Categories.AddRange(categories);
            await db.SaveChangesAsync(); // để có CategoryId

            var foods = new List<FastFood>();

            // Category 1: Hamburger (ID: categories[0].CategoryId)
            foods.AddRange(new[]
            {
                new FastFood { Name = "Burger Bò", Description = "Burger bò đặc biệt", Price = 55_000, CategoryId = categories[0].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Burger Gà", Description = "Burger gà giòn cay", Price = 49_000, CategoryId = categories[0].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Burger Phô Mai", Description = "Burger phô mai tan chảy", Price = 60_000, CategoryId = categories[0].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Burger Trứng", Description = "Burger bò + trứng", Price = 52_000, CategoryId = categories[0].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Burger Siêu To", Description = "Combo burger khổng lồ", Price = 85_000, CategoryId = categories[0].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" }
            });

            // Category 2: Pizza
            foods.AddRange(new[]
            {
                new FastFood { Name = "Pizza Hải Sản", Description = "Pizza hải sản tươi ngon", Price = 120_000, CategoryId = categories[1].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Pizza Bò", Description = "Pizza bò phô mai", Price = 115_000, CategoryId = categories[1].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Pizza Gà", Description = "Pizza gà BBQ", Price = 110_000, CategoryId = categories[1].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Pizza Phô Mai", Description = "Pizza 4 loại phô mai", Price = 130_000, CategoryId = categories[1].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Pizza Rau Củ", Description = "Pizza chay healthy", Price = 90_000, CategoryId = categories[1].CategoryId, ImageUrl ="http://localhost:5062/images/foods/1.png" }
            });

            // Category 3: Drinks
            foods.AddRange(new[]
            {
                new FastFood { Name = "Coca Cola", Description = "Đồ uống có gas", Price = 15_000, CategoryId = categories[2].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Pepsi", Description = "Pepsi lạnh", Price = 15_000, CategoryId = categories[2].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Trà Chanh", Description = "Trà chanh tươi", Price = 20_000, CategoryId = categories[2].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Trà Sữa", Description = "Trà sữa trân châu", Price = 35_000, CategoryId = categories[2].CategoryId, ImageUrl = "http://localhost:5062/images/foods/1.png" },
                new FastFood { Name = "Cam Ép", Description = "Nước cam tươi", Price = 25_000, CategoryId = categories[2].CategoryId, ImageUrl ="http://localhost:5062/images/foods/1.png" }
            });

            db.FastFoods.AddRange(foods);
            await db.SaveChangesAsync();
        }
        if (!db.Combos.Any())
        {
            var combos = new List<Combo>
            {
                new Combo
                {
                    ComboId = 1,
                    Name = "Combo Gà Rán Siêu Rẻ",
                    Description = "Gồm 2 miếng gà + 1 khoai tây + 1 coca",
                    Price = 79000,
                    ImageUrl = "/images/combo1.jpg",
                    ComboItems = new List<ComboItem>
                    {
                        new ComboItem { ComboItemId = 1, FastFoodId = 1, Quantity = 2 },
                        new ComboItem { ComboItemId = 2, FastFoodId = 5, Quantity = 1 },
                        new ComboItem { ComboItemId = 3, FastFoodId = 8, Quantity = 1 }
                    }
                },

                new Combo
                {
                    ComboId = 2,
                    Name = "Combo Burger Thịnh Soạn",
                    Description = "1 burger bò + 1 khoai tây + 1 nước tự chọn",
                    Price = 99000,
                    ImageUrl = "/images/combo2.jpg",
                    ComboItems = new List<ComboItem>
                    {
                        new ComboItem { ComboItemId = 4, FastFoodId = 3, Quantity = 1 },
                        new ComboItem { ComboItemId = 5, FastFoodId = 6, Quantity = 1 },
                        new ComboItem { ComboItemId = 6, FastFoodId = 9, Quantity = 1 }
                    }
                },

                new Combo
                {
                    ComboId = 3,
                    Name = "Combo Gia Đình",
                    Description = "3 phần gà + 2 burger + 2 nước lớn",
                    Price = 199000,
                    ImageUrl = "/images/combo3.jpg",
                    ComboItems = new List<ComboItem>
                    {
                        new ComboItem { ComboItemId = 7, FastFoodId = 1, Quantity = 3 },
                        new ComboItem { ComboItemId = 8, FastFoodId = 3, Quantity = 2 },
                        new ComboItem { ComboItemId = 9, FastFoodId = 8, Quantity = 2 }
                    }
                },

                new Combo
                {
                    ComboId = 4,
                    Name = "Combo Pizza Noon",
                    Description = "1 pizza nhỏ + 1 mì Ý + 1 nước ngọt",
                    Price = 159000,
                    ImageUrl = "/images/combo4.jpg",
                    ComboItems = new List<ComboItem>
                    {
                        new ComboItem { ComboItemId = 10, FastFoodId = 10, Quantity = 1 },
                        new ComboItem { ComboItemId = 11, FastFoodId = 11, Quantity = 1 },
                        new ComboItem { ComboItemId = 12, FastFoodId = 8, Quantity = 1 }
                    }
                },

                new Combo
                {
                    ComboId = 5,
                    Name = "Combo Ăn Nhẹ Văn Phòng",
                    Description = "1 sandwich + 1 trà đào + 1 salad nhỏ",
                    Price = 69000,
                    ImageUrl = "/images/combo5.jpg",
                    ComboItems = new List<ComboItem>
                    {
                        new ComboItem { ComboItemId = 13, FastFoodId = 12, Quantity = 1 },
                        new ComboItem { ComboItemId = 14, FastFoodId = 13, Quantity = 1 },
                        new ComboItem { ComboItemId = 15, FastFoodId = 14, Quantity = 1 }
                    }
                },
            };
            db.Combos.AddRange(combos);
            await db.SaveChangesAsync();

        }
    }
}