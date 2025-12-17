using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AngleSharp.Css.Dom;
using Ganss.Xss;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
builder.Services.AddScoped<ICategoryServices, CategoryServices>();
builder.Services.AddScoped<IFastFoodServices, FastFoodServices>();
builder.Services.AddScoped<IComboServices, ComboServices>();
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IOrderServices, OrderServices>();
builder.Services.AddScoped<IAccountServices, AccountServices>();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginPolicy", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("anonymous", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("authenticated", options =>
    {
        options.PermitLimit = 500;
        options.Window = TimeSpan.FromMinutes(5);
    });
    options.AddFixedWindowLimiter("admin", options =>
    {
        options.PermitLimit = 1000;
        options.Window = TimeSpan.FromMinutes(1);
    });
    
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.", cancellationToken);
    };
});

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
    // Tạo file XML để lấy comment mô tả
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
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
app.UseCors();

app.UseRateLimiter();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


// FastFood Endpoints
var foodEndpoints = app.MapGroup("/api/fastfoods").DisableAntiforgery();
foodEndpoints.MapGet("/", async (IFastFoodServices services) =>
{
    var foods = await services.GetAllFastFoodsAsync();
    return Results.Ok(foods);
}).RequireRateLimiting("anonymous");
foodEndpoints.MapGet("/admin", async (IFastFoodServices services) =>
{
    var foods = await services.GetAllFastFoodsByAdminAsync();
    return Results.Ok(foods);
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
foodEndpoints.MapGet("/{id:int}", async (int id, IFastFoodServices services) =>
{
    var food = await services.GetFastFoodByIdAsync(id);
    if (food == null)
    {
        return Results.NotFound(new { Message = "Fast food item not found." });
    }
    return Results.Ok(food);
}).RequireRateLimiting("anonymous");
foodEndpoints.MapPost("/", async ([FromForm] FastFoodDto dto, IFastFoodServices services, HttpContext context) =>
{
    try
    {
        if (dto.ImageFile == null || dto.ImageFile.Length == 0)
        {
            return Results.BadRequest(new { error = "Không có file" });
        }
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);
        if (!isValid)
        {
            return Results.BadRequest(new
            {
                Errors = validationResults.Select(v => v.ErrorMessage)
            });
        }
        var food = await services.CreateFastFoodAsync(dto, context);
        return Results.Created($"/api/fastfoods/{food.FastFoodId}", food);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error creating fast food item.", Details = ex.Message });
    }
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }).WithOpenApi(operation => new(operation)
{
    Summary = "Tạo món ăn nhanh mới",
    Description = "Tạo món ăn nhanh mới, chỉ Admin mới có quyền thực hiện hành động này."
});
foodEndpoints.MapPut("/{id}", async (int id, [FromForm] FastFoodDto dto, IFastFoodServices services, HttpContext context) =>
{
    try
    {
        var existingFastFood = await services.GetFastFoodByIdAsync(id);
        if (existingFastFood is null)
        {
            return Results.NotFound(new { Message = $"Không tìm thấy FastFood có Id là {id}" });
        }
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);
        if (!isValid)
        {
            return Results.BadRequest(new
            {
                Errors = validationResults.Select(v => v.ErrorMessage)
            });
        }
        var food = await services.UpdateFastFoodAsync(existingFastFood,dto,context);
        return Results.Ok(food);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error updating fast food item.", Details = ex.Message });
    }
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
foodEndpoints.MapDelete("/{id}", async (int id, IFastFoodServices services) =>
{
    try
    {
        var existingFood = await services.GetFastFoodByIdAsync(id);
        if (existingFood == null)
        {
            return Results.NotFound(new { Message = "Fast food item not found." });
        }

        await services.DeleteFastFoodAsync(existingFood);
        return Results.Ok(new { Message = "Fast food item deleted successfully." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error deleting fast food item.", Details = ex.Message });
    }
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }); ;
foodEndpoints.MapPut("/lock/{id}", async (int id, IFastFoodServices services) =>
{
    try
    {
        var existingFood = await services.GetFastFoodByIdAsync(id);
        if (existingFood == null)
        {
            return Results.NotFound(new { Message = "Food not found." });

        }

        await services.LockFastFoodAsync(existingFood);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error updating combo.", Details = ex.Message });
    }
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });


// Combo Endpooints
var comboEndpoints = app.MapGroup("/api/combos");
comboEndpoints.MapGet("/", async (IComboServices services) =>
{
    var combos = await services.GetAllCombosAsync();
    return Results.Ok(combos);
}).RequireRateLimiting("anonymous");
comboEndpoints.MapGet("/admin", async (IComboServices services) =>
{
    var combos = await services.GetAllCombosByAdminAsync();
    return Results.Ok(combos);
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
comboEndpoints.MapGet("/{id:int}", async (int id, IComboServices services) =>
{
    var combo = await services.GetComboByIdAsync(id);
    if (combo == null)
    {
        return Results.NotFound(new { Message = "Combo not found." });
    }
    return Results.Ok(combo);
}).RequireRateLimiting("anonymous");
comboEndpoints.MapPost("/", async ([FromForm] ComboDto dto, IComboServices services, HttpContext context) =>
{
    try
    {
        if (dto.ImageFile == null || dto.ImageFile.Length == 0)
        {
            return Results.BadRequest(new { error = "Không có file" });
        }
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);
        if (!isValid)
        {
            return Results.BadRequest(new
            {
                Errors = validationResults.Select(v => v.ErrorMessage)
            });
        }

        var comboCreated = await services.CreateComboAsync(dto, context);
        return Results.Created($"/api/combos/{comboCreated?.ComboId}", comboCreated);
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
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }).DisableAntiforgery();
comboEndpoints.MapPut("/{id}",async (int id, [FromForm]ComboDto dto, IComboServices services, HttpContext context) =>
{
    try
    {
        var existingCombo = await services.GetComboByIdAsync(id);
        if (existingCombo is null)
        {
            return Results.NotFound(new { Message = $"Không tìm thấy combo có Id là {id}" });
        }
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);
        if (!isValid)
        {
            return Results.BadRequest(new
            {
                Errors = validationResults.Select(v => v.ErrorMessage)
            });
        }
        
        var comboUpdated = await services.UpdateComboAsync(existingCombo,dto,context);
        return Results.Ok(comboUpdated);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error updating combo.", Details = ex.Message });
    }
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }).DisableAntiforgery();
comboEndpoints.MapDelete("/{id}", async(int id, IComboServices services) =>
{
    try
    {
        var existingCombo = await services.GetComboByIdAsync(id);
        if (existingCombo == null)
        {
            return Results.NotFound(new { Message = "Combo not found." });
        }
        
        await services.DeleteComboAsync(existingCombo);

        return Results.Ok(new { Message = "Combo deleted successfully." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error deleting Combo.", Details = ex.Message });
    }
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }); ;
comboEndpoints.MapPut("/lock/{id}", async (int id, IComboServices services) =>
{
    try
    {
        var existingCombo = await services.GetComboByIdAsync(id);
        if (existingCombo == null)
        {
            return Results.NotFound(new { Message = "Combo not found." });
        }

        await services.LockComboAsync(existingCombo);

        return Results.Ok(existingCombo);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error updating combo.", Details = ex.Message });
    }
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });


// Order Endpoints
var orderEndpoint = app.MapGroup("/api/orders");
orderEndpoint.MapGet("/",async (IOrderServices services) =>
{
    var orders = await services.GetAllOrdersAsync();
    return Results.Ok(orders);
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute {Roles = "Admin"});
orderEndpoint.MapGet("/{id}",async (int id, IOrderServices services) =>
{

    var orders = await services.GetOrderByIdAsync(id);
    return Results.Ok(orders);

}).RequireRateLimiting("anonymous");
orderEndpoint.MapPost("/",async (Order newOrder, IOrderServices services) =>
{
    try
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(newOrder);
        bool isValid = Validator.TryValidateObject(newOrder, validationContext, validationResults, true);
        if (!isValid)
        {
            return Results.BadRequest(new
            {
                Errors = validationResults.Select(v => v.ErrorMessage)
            });
        }
        await services.CreateOrderAsync(newOrder);
        return Results.Created($"/api/combos/{newOrder.OrderId}", newOrder);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error creating Order.", Details = ex.Message });
    }
}).RequireRateLimiting("admin");
orderEndpoint.MapPut("/{id}",async (int id,OrderStatusDto orderStatus, IOrderServices services,AppDbContext db) =>
{
    try
    {
        var existingOrder = await db.Orders.FindAsync(id);
        if (existingOrder == null)
        {
            return Results.NotFound(new { Message = "Order not found." });
        }
        if (!Enum.TryParse<OrderStatus>(orderStatus.Status, ignoreCase: true, out var status))
        {
            return Results.BadRequest(new { Message = "Trạng thái không hợp lệ. Chỉ chấp nhận: Pending, Preparing, Delivering, Completed, Cancelled" });
        }
        
        await services.UpdateOrderAsync(existingOrder, status);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Message = "Error updating Order.", Details = ex.Message });
    }
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }); ;
// orderEndpoint.MapDelete("/{id}",async (int id, IOrderServices services) =>
// {
//     try
//     {
//         var existingOrder = await db.Orders.FindAsync(id);
//         if (existingOrder == null)
//         {
//             return Results.NotFound(new { Message = "OrexistingOrder not found." });
//         }
//         db.Orders.Remove(existingOrder);
//         await db.SaveChangesAsync();
//         return Results.Ok(new { Message = "Combo deleted successfully." });
//     }
//     catch (Exception ex)
//     {
//         return Results.BadRequest(new { Message = "Error deleting Combo.", Details = ex.Message });
//     }
// });


// User Endpoints
var userEndpoint = app.MapGroup("/api/users").RequireAuthorization().RequireRateLimiting("authenticated");
userEndpoint.MapGet("/", async (IUserServices services) =>
{
    return Results.Ok(await services.GetAllUsersAsync());
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
userEndpoint.MapGet("/{id}", async (int id, IUserServices services) =>
{
    var user = await services.GetUserByIdAsync(id);
    if (user is null)
    {
        return Results.NotFound(new { Message = $"Không tìm thấy người dùng có Id là {id}" });
    }
    return Results.Ok(user);
});
userEndpoint.MapPost("/", async (User newUser, IUserServices services) =>
{
    try
    {
        var user = await services.GetUserByEmailAsync(newUser.Email);
        if (user != null)
        {
            return Results.BadRequest("Email đã tồn tại vui lòng sử dụng email khác");
        }
        await services.CreateUserAsync(newUser);
        return Results.Created($"/api/users/{newUser.UserId}", newUser);

    }
    catch (Exception e)
    {
        return Results.Problem(e.Message, "Có lỗi xảy ra khi thêm người dùng mới");
    }
}).RequireRateLimiting("admin").WithParameterValidation().RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
userEndpoint.MapPut("/{id}", async (int id, IUserServices services, User updateUser) =>
{
    try
    {
        if (updateUser.UserId != id)
        {
            return Results.BadRequest($"Id không khớp nhau Route = {id}, body = {updateUser.UserId}");
        }
        var user = await services.GetUserByIdAsync(id);
        if (user is null)
        {
            return Results.BadRequest($"Không tìm thấy user có Id là {id}");
        }
        await services.UpdateUserAsync(user,updateUser);
        return Results.Ok(updateUser);
    }
    catch (Exception e)
    {
        return Results.Problem(e.Message, "Lỗi khi cập nhật thông tin người dùng");
    }
});
userEndpoint.MapDelete("/{id}", async (int id, IUserServices services, HttpContext context) =>
{
    var user = await services.GetUserByIdAsync(id);
    if (user is null)
    {
        return Results.NotFound(new { Message = $"Không tìm thấy user có id là {id}" });
    }
    var userId = context.User.FindFirst("Id")?.Value;
    if (user.UserId.ToString() == userId)
    {
        return Results.BadRequest(new { Message = "Không thể xóa tài khoản đang đăng nhập" });
    }

    await services.DeleteUserAsync(user);
    return Results.NoContent();
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
userEndpoint.MapPut("/lock/{id}", async (int id, IUserServices services, HttpContext context) =>
{
    try
    {
        var user = await services.GetUserByIdAsync(id);
        if (user is null)
        {
            return Results.BadRequest(new { Message = $"Không tìm thấy user có Id là {id}" });
        }
        var userId = context.User.FindFirst("Id")?.Value;
        if (user.UserId.ToString() == userId)
        {
            return Results.BadRequest(new { Message = $"Không khóa tài khoản đang đăng nhập" });
        }
        
        await services.LockUserAsync(user);
        return Results.NoContent();
    }
    catch (Exception e)
    {
        return Results.Problem(e.Message, "Lỗi khi khóa/mở khóa người dùng");
    }
}).RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });


// Category Endpoints
var categories = app.MapGroup("/api/categories").RequireRateLimiting("admin").RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
categories.MapGet("/", async (ICategoryServices services) =>
{
    var categories = await services.GetAllCategoriesAsync();
    return Results.Ok(categories);
});
categories.MapGet("/{id}", async (int id, ICategoryServices services) =>
{
    var category = await services.GetCategoryByIdAsync(id);
    return category is null ? Results.NotFound("Category not found") : Results.Ok(category);
});
categories.MapPost("/", async (Category newCategory, ICategoryServices services) =>
{
    // Validate
    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(newCategory);

    if (!Validator.TryValidateObject(newCategory, context, validationResults, true))
    {
        return Results.BadRequest(validationResults.Select(v => v.ErrorMessage));
    }

    var category = await services.CreateCategoryAsync(newCategory);

    return Results.Created($"/api/categories/{category.CategoryId}", category);
});
categories.MapPut("/{id}", async (int id, Category updated, ICategoryServices services) =>
{
    var category = await services.GetCategoryByIdAsync(id);
    if (category is null)
        return Results.NotFound("Category not found");

    await services.UpdateCategoryAsync(category,updated);

    return Results.Ok(category);
});
categories.MapDelete("/{id}", async (int id, ICategoryServices services) =>
{
    var category = await services.GetCategoryByIdAsync(id);
    if (category is null)
        return Results.NotFound("Category not found");

    await services.DeleteCategoryAsync(category);

    return Results.Ok(new { message = "Deleted successfully" });
});


// Login and Register Endpoints
app.MapPost("/api/login", async (LoginRequest login, IAccountServices services, IConfiguration _configuration) =>
{
    try
    {
        if(login.Email is null || login.Password is null)
        {
            return Results.BadRequest(new { Message = "Vui lòng nhập đầy đủ thông tin đăng nhập" });
        }
        var user = await services.GetCurrentUserAsync(login.Email);
        if (user is null)
        {
            return Results.BadRequest(new { Message = $"{login.Email} không tồn tại vui lòng kiểm tra lại Email" });
        }
        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, login.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Results.BadRequest(new { Message = "Mật khẩu không đúng vui lòng thử lại" });
        }
        if (!user.IsActive)
        {
            return Results.BadRequest(new { Message = "Tài khoản của bạn đã bị khóa vui lòng liên hệ theo hotline: 0987654321 để dc hỗ trợ" });
        }

        var newToken = await services.LoginAsync(login, user);

        return Results.Ok(new
        {
            token = newToken,
            role = user.UserRole.ToString(),
            userName = user.Username,
            userId = user.UserId
        });
    }
    catch(Exception e)
    {
        return Results.Problem(e.Message, "Có lỗi xảy ra khi đăng nhập vui lòng thử lại sau");
    }
}).RequireRateLimiting("LoginPolicy");
app.MapPost("/api/register", async (User user, IAccountServices services, IHtmlSanitizer sanitizer) =>
{
    try
    {
        var existingUser = await services.GetCurrentUserAsync(user.Email);
        if (existingUser != null)
        {
            return Results.BadRequest(new { Message = "Email đã tồn tại vui lòng sử dụng email khác" });
        }
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

        await services.RegisterAsync(user);

        return Results.NoContent();
    }
    catch(Exception e)
    {
        return Results.Problem(e.Message, "Có lỗi xảy ra khi đăng ký tài khoản");
    }
}).RequireRateLimiting("LoginPolicy").WithParameterValidation();


// Add seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.Initialize(db);
}

app.Run();

