using lab2.Data;
using lab2.Models;
using lab2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services; //email sender interface
using lab2.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký HttpContextAccessor (Cần thiết cho các xử lý liên quan đến User)
builder.Services.AddHttpContextAccessor();

// 2. Cấu hình Ngôn ngữ & Tiền tệ (vi-VN)
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new System.Globalization.CultureInfo("vi-VN") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("vi-VN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Đăng ký EmailSender (Sử dụng MailKit để gửi email qua Gmail SMTP)
builder.Services.AddTransient<IEmailSender, EmailSender>();


// Cho phép UserName chứa khoảng trắng và các ký tự đặc biệt để làm "Tên hiển thị"
builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.AllowedUserNameCharacters = null;
    // Đảm bảo Email là duy nhất
    options.User.RequireUniqueEmail = true;
});

// 3. Cấu hình Cookie Đăng nhập
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Đăng ký DbContext (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Connection1")));

// 5. Đăng ký Identity (User + Role)
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// 6. Đăng ký Repository & Services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// 7. Đăng ký MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 8. Seed Role + Admin User (Giữ nguyên logic của bạn)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    var adminUser = await userManager.FindByEmailAsync("admin@lifeandtrees.com");
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = "admin123", Email = "admin@lifeandtrees.com" };
        var result = await userManager.CreateAsync(adminUser, "Admin@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// 9. Pipeline (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 10. Cấu hình Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();