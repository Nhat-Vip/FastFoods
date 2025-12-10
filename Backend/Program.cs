using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Ganss.Xss;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true; //Lưu Token vào HttpContext để dùng 
    // VD: var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,// kiểm tra ai phát hành token
        ValidateAudience = true,// kiểm tra token này dùng cho ai
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
builder.Services.AddAuthorization();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddSingleton<IHtmlSanitizer, HtmlSanitizer>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Quản lý website bán đồ ăn nhanh",
        Version = "v1",
        Description = "API cho website quản lý bán đồ ăn nhanh"

    });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = @"Nhập token theo dạng: <strong>Bearer {token}</strong><br/>
                        Ví dụ: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});
builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();
app.UseCors();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var foodEndpoints = app.MapGroup("/api/fastfoods").DisableAntiforgery();
// FastFood Endpoints
foodEndpoints.MapGet("/", async (AppDbContext db) =>
{
    var foods = await db.FastFoods
        .Include(ff => ff.Category)
        .Where(ff => ff.IsActive)
        .ToListAsync();
    return Results.Ok(foods);
});
foodEndpoints.MapGet("/admin", async (AppDbContext db) =>
{
    var foods = await db.FastFoods
        .Include(ff => ff.Category)
        .ToListAsync();
    return Results.Ok(foods);
});

foodEndpoints.MapGet("/{id:int}", async (int id, AppDbContext db) =>
{
    var food = await db.FastFoods
        .Include(ff => ff.Category)
        .FirstOrDefaultAsync(ff => ff.FastFoodId == id && ff.IsActive);
    if (food == null)
    {
        return Results.NotFound(new { Message = "Fast food item not found." });
    }
    return Results.Ok(food);
});
foodEndpoints.MapPost("/", async ([FromForm] FastFoodDto dto, AppDbContext db, HttpContext context) =>
{
    try
    {
        if (dto.ImageFile == null || dto.ImageFile.Length == 0)
            return Results.BadRequest(new { error = "Không có file" });
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
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            ImageUrl = imageUrl
        };


        db.FastFoods.Add(food);
        await db.SaveChangesAsync();
        return Results.Created($"/api/fastfoods/{food.FastFoodId}", food);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error creating fast food item.", Details = ex.Message });
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
foodEndpoints.MapPut("/{id}", async (int id, [FromForm] FastFoodDto dto, AppDbContext db,HttpContext context) =>
{
    try
    {
        var existingFastFood = await db.FastFoods.FindAsync(id);
        if (existingFastFood is null)
        {
            return Results.NotFound(new { Message = $"Không tìm thấy FastFood có Id là {id}" });
        }
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
            existingFastFood.ImageUrl = imageUrl;
        }

        existingFastFood.Name = dto.Name!;
        existingFastFood.Description = dto.Description!;
        existingFastFood.Price = dto.Price;
        existingFastFood.CategoryId = dto.CategoryId;

        await db.SaveChangesAsync();
        return Results.Ok(existingFastFood);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error updating fast food item.", Details = ex.Message });
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
foodEndpoints.MapDelete("/{id}", async (int id, AppDbContext db) =>
{
    try
    {
        var existingFood = await db.FastFoods.FindAsync(id);
        if (existingFood == null)
        {
            return Results.NotFound(new { Message = "Fast food item not found." });
        }

        db.FastFoods.Remove(existingFood);
        await db.SaveChangesAsync();
        return Results.Ok(new { Message = "Fast food item deleted successfully." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error deleting fast food item.", Details = ex.Message });
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }); ;
foodEndpoints.MapPut("/lock/{id}", async (int id, AppDbContext db) =>
{
    try
    {
        var existingFood = await db.FastFoods.FindAsync(id);
        if (existingFood == null)
        {
            return Results.NotFound(new { Message = "Food not found." });
        }

        existingFood.IsActive = !existingFood.IsActive;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error updating combo.", Details = ex.Message });
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });


// Combo Endpooints
var comboEndpoints = app.MapGroup("/api/combos");
comboEndpoints.MapGet("/", async (AppDbContext db) =>
{
    var combos = await db.Combos
        .Include(c => c.ComboItems)
            .ThenInclude(ci => ci.FastFood)
        .Where(c => c.IsActive)
        .ToListAsync();
    return Results.Ok(combos);
});
comboEndpoints.MapGet("/admin", async (AppDbContext db) =>
{
    var combos = await db.Combos
        .Include(c => c.ComboItems)
            .ThenInclude(ci => ci.FastFood)
        .ToListAsync();
    return Results.Ok(combos);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

comboEndpoints.MapGet("/{id:int}", async (int id, AppDbContext db) =>
{
    var combo = await db.Combos
        .Include(c => c.ComboItems)
            .ThenInclude(ci => ci.FastFood)
        .FirstOrDefaultAsync(c => c.ComboId == id && c.IsActive);
    if (combo == null)
    {
        return Results.NotFound(new { Message = "Combo not found." });
    }
    return Results.Ok(combo);
});

comboEndpoints.MapPost("/", async ([FromForm] ComboDto dto, AppDbContext db, HttpContext context) =>
{
    try
    {
        if (dto.ImageFile == null || dto.ImageFile.Length == 0)
            return Results.BadRequest(new { error = "Không có file" });
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
        // Console.WriteLine("AAAAAAAAAAAAAAA");
        db.Combos.Add(combo);
        await db.SaveChangesAsync();
        var comboCreated = db.Combos.Include(c => c.ComboItems)
            .ThenInclude(ci => ci.FastFood)
            .Where(c => c.ComboId == combo.ComboId);
        return Results.Created($"/api/combos/{combo.ComboId}", comboCreated);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            Message = "Error creating combo.",
            Details = ex.Message,
            Inner = ex.InnerException?.Message
        });
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }).DisableAntiforgery();
comboEndpoints.MapPut("/{id}",async (int id, [FromForm]ComboDto dto, AppDbContext db, HttpContext context) =>
{
    try
    {
        var existingCombo = await db.Combos.Include(c=>c.ComboItems).FirstOrDefaultAsync(c=>c.ComboId == id);
        if(existingCombo is null)
        {
            return Results.NotFound(new { Message = $"Không tìm thấy combo có Id là {id}" });
        }
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
            existingCombo.ImageUrl = imageUrl;
        }
        db.ComboItems.RemoveRange(existingCombo.ComboItems);
        existingCombo.Description = dto.Description!;
        existingCombo.Name = dto.Name!;
        existingCombo.Price = dto.Price;
        existingCombo.ComboItems = JsonSerializer.Deserialize<List<ComboItem>>(dto.ComboItems!)!;

        await db.SaveChangesAsync();
        var comboUpdated = db.Combos.Include(c => c.ComboItems)
            .ThenInclude(ci => ci.FastFood)
            .Where(c => c.ComboId == existingCombo.ComboId);
        return Results.Ok(comboUpdated);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error updating combo.", Details = ex.Message });
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }).DisableAntiforgery();
comboEndpoints.MapDelete("/{id}", async(int id, AppDbContext db) =>
{
    try
    {
        var existingCombo = await db.Combos.FindAsync(id);
        if (existingCombo == null)
        {
            return Results.NotFound(new { Message = "Combo not found." });
        }
        db.Combos.Remove(existingCombo);
        await db.SaveChangesAsync();
        return Results.Ok(new { Message = "Combo deleted successfully." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error deleting Combo.", Details = ex.Message });
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }); ;
comboEndpoints.MapPut("/lock/{id}", async (int id, AppDbContext db) =>
{
    try
    {
        var existingCombo = await db.Combos.FindAsync(id);
        if (existingCombo == null)
        {
            return Results.NotFound(new { Message = "Combo not found." });
        }

        existingCombo.IsActive = !existingCombo.IsActive;

        await db.SaveChangesAsync();
        return Results.Ok(existingCombo);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error updating combo.", Details = ex.Message });
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

var orderEndpoint = app.MapGroup("/api/orders");

orderEndpoint.MapGet("/",async (AppDbContext db) =>
{
    var today = DateTime.Today;            // 00:00 hôm nay
    var yesterday = DateTime.Today.AddDays(-1); // 00:00 hôm qua

    var orders = await db.Orders
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.FastFood)
    .Include(o => o.User)
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Combo)
            .ThenInclude(c => c.ComboItems)
                .ThenInclude(ci=>ci.FastFood)
    .Where(o => o.OrderDate >= yesterday && o.OrderDate < today.AddDays(1))
    .ToListAsync();
    return Results.Ok(orders);
}).RequireAuthorization(new AuthorizeAttribute{Roles = "Admin"});

orderEndpoint.MapGet("/{id}",async (int id, AppDbContext db) =>
{

    var orders = await db.Orders
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.FastFood)
    .Include(o => o.User)
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Combo)
            .ThenInclude(c => c.ComboItems)
                .ThenInclude(ci => ci.FastFood)
    .Where(o=>o.UserId == id)
    .ToListAsync();
    return Results.Ok(orders);

});
orderEndpoint.MapPost("/",async (Order newOrder, AppDbContext db) =>
{
    try
    {
        db.Orders.Add(newOrder);
        await db.SaveChangesAsync();
        return Results.Created($"/api/combos/{newOrder.OrderId}", newOrder);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error creating Order.", Details = ex.Message });
    }
});

orderEndpoint.MapPut("/{id}",async (int id,OrderStatusDto orderStatus, AppDbContext db) =>
{
    try
    {
        var existingOrder = await db.Orders.FindAsync(id);
        if (existingOrder == null)
        {
            return Results.NotFound(new { Message = "Order not found." });
        }
        Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAAAAAA" + orderStatus.Status);
        if (!Enum.TryParse<OrderStatus>(orderStatus.Status, ignoreCase: true, out var status))
        {
            return Results.BadRequest(new { Message = "Trạng thái không hợp lệ. Chỉ chấp nhận: Pending, Preparing, Delivering, Completed, Cancelled" });
        }
        existingOrder.Status = status;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error updating Order.", Details = ex.Message });
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }); ;

orderEndpoint.MapDelete("/{id}",async (int id, AppDbContext db) =>
{
    try
    {
        var existingOrder = await db.Orders.FindAsync(id);
        if (existingOrder == null)
        {
            return Results.NotFound(new { Message = "OrexistingOrder not found." });
        }
        db.Orders.Remove(existingOrder);
        await db.SaveChangesAsync();
        return Results.Ok(new { Message = "Combo deleted successfully." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error deleting Combo.", Details = ex.Message });
    }
});


var userEndpoint = app.MapGroup("/api/users").RequireAuthorization();

userEndpoint.MapGet("/", async (AppDbContext db) =>
{
    return Results.Ok(await db.Users.ToListAsync());
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
userEndpoint.MapGet("/{id}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null)
    {
        return Results.NotFound(new { Message = $"Không tìm thấy người dùng có Id là {id}" });
    }
    return Results.Ok(user);
});
userEndpoint.MapPost("/", async (User newUser, AppDbContext db) =>
{
    try
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == newUser.Email);
        var hasher = new PasswordHasher<User>();
        newUser.PasswordHash = hasher.HashPassword(newUser, newUser.PasswordHash);
        if (user != null)
        {
            return Results.BadRequest("Email đã tồn tại vui lòng sử dụng email khác");
        }
        db.Users.Add(newUser);
        await db.SaveChangesAsync();
        return Results.Created($"/api/users/{newUser.UserId}", newUser);

    }
    catch (Exception e)
    {
        return Results.Problem(e.Message, "Có lỗi xảy ra khi thêm người dùng mới");
    }
}).WithParameterValidation().RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
userEndpoint.MapPut("/{id}", async (int id, AppDbContext db, User updateUser) =>
{
    try
    {
        if (updateUser.UserId != id)
        {
            return Results.BadRequest($"Id không khớp nhau Route = {id}, body = {updateUser.UserId}");
        }
        var user = await db.Users.FindAsync(id);
        if (user is null)
        {
            return Results.BadRequest($"Không tìm thấy user có Id là {id}");
        }
        user.Username = updateUser.Username;
        user.Email = updateUser.Email;
        user.FullName = updateUser.FullName;
        user.Phone = updateUser.Phone;
        user.Address = updateUser.Address;
        user.DateOfBirth = updateUser.DateOfBirth;

        await db.SaveChangesAsync();
        return Results.Ok(updateUser);
    }
    catch (Exception e)
    {
        return Results.Problem(e.Message, "Lỗi khi cập nhật thông tin người dùng");
    }
});
userEndpoint.MapDelete("/{id}", async (int id, AppDbContext db, HttpContext context) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null)
    {
        return Results.NotFound(new { Message = $"Không tìm thấy user có id là {id}" });
    }
    var userId = context.User.FindFirst("Id")?.Value;
    if(user.UserId.ToString() == userId)
    {
        return Results.BadRequest(new { Message = "Không thể xóa tài khoản đang đăng nhập" });
    }
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
userEndpoint.MapPut("/lock/{id}", async (int id, AppDbContext db, HttpContext context) =>
{
    try
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
        {
            return Results.BadRequest(new { Message = $"Không tìm thấy user có Id là {id}" });
        }
        var userId = context.User.FindFirst("Id")?.Value;
        if (user.UserId.ToString() == userId)
        {
            return Results.BadRequest(new { Message =  $"Không khóa tài khoản đang đăng nhập" });
        }
        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (Exception e)
    {
        return Results.Problem(e.Message, "Lỗi khi khóa/mở khóa người dùng");
    }
});


var categories = app.MapGroup("/api/categories");
categories.MapGet("/", async (AppDbContext db) =>
{
    var categories = await db.Categories.Include(c=>c.FastFoods).ToListAsync();
    return Results.Ok(categories);
});
categories.MapGet("/{id}", async (int id, AppDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    return category is null ? Results.NotFound("Category not found") : Results.Ok(category);
});
categories.MapPost("/", async (Category newCategory, AppDbContext db) =>
{
    // Validate
    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(newCategory);

    if (!Validator.TryValidateObject(newCategory, context, validationResults, true))
    {
        return Results.BadRequest(validationResults.Select(v => v.ErrorMessage));
    }

    await db.Categories.AddAsync(newCategory);
    await db.SaveChangesAsync();

    return Results.Created($"/api/categories/{newCategory.CategoryId}", newCategory);
});
categories.MapPut("/{id}", async (int id, Category updated, AppDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null)
        return Results.NotFound("Category not found");

    category.CategoryName = updated.CategoryName;
    category.Description = updated.Description;

    await db.SaveChangesAsync();
    return Results.Ok(category);
});
categories.MapDelete("/{id}", async (int id, AppDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);
    if (category is null)
        return Results.NotFound("Category not found");

    db.Categories.Remove(category);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Deleted successfully" });
});






app.MapPost("/api/login", async (LoginRequest login, AppDbContext db, IConfiguration _configuration) =>
{
    try
    {
        var Email = login.Email;
        var Password = login.Password;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == Email);
        var hasher = new PasswordHasher<User>();
        if (user is null)
        {
            return Results.BadRequest(new { Message = $"{Email} không tồn tại vui lòng kiểm tra lại Email" });
        }
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Results.BadRequest(new { Message = "Mật khẩu không đúng vui lòng thử lại" });
        }
        if (!user.IsActive)
        {
            return Results.BadRequest(new { Message = "Tài khoản của bạn đã bị khóa vui lòng liên hệ theo hotline: 0987654321 để dc hỗ trợ" });
        }
        var claims = new[]
        {
            new Claim (JwtRegisteredClaimNames.Sub,user.UserId.ToString()),
            new Claim (JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
            new Claim (JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new Claim ("Id",user.UserId.ToString()),
            new Claim ("UserName",user.Username),
            new Claim ("Email", user.Email),
            new Claim (ClaimTypes.Role,user.UserRole.ToString())

        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: signIn
        );
        return Results.Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            role = user.UserRole.ToString(),
            userName = user.Username,
            userId = user.UserId
        });
    }
    catch(Exception e)
    {
        return Results.Problem(e.Message, "Có lỗi xảy ra khi đăng nhập vui lòng thử lại sau");
    }
});


app.MapPost("/api/register", async (User user, AppDbContext db, IHtmlSanitizer sanitizer) =>
{
    try
    {
        user.Address = sanitizer.Sanitize(user.Address);
        user.FullName = sanitizer.Sanitize(user.FullName);
        user.Username = sanitizer.Sanitize(user.Username);
        user.UserRole = UserRole.Customer;
        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(user);
        bool isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);
        if (!isValid)
        {
            return Results.BadRequest(new
            {
                Errors = validationResults.Select(v => v.ErrorMessage)
            });
        }
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch(Exception e)
    {
        return Results.Problem(e.Message, "Có lỗi xảy ra khi đăng ký tài khoản");
    }
}).WithParameterValidation();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.Initialize(db);
}

app.Run();

