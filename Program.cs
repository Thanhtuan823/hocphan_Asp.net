using lab2.Data;
using lab2.Models;
using lab2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Thêm vào trước dòng var app = builder.Build();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new System.Globalization.CultureInfo("vi-VN") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("vi-VN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Đăng ký SessionCart (giỏ hàng)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian sống của session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<SessionCart>(sp => SessionCart.GetCart(sp));

//Cookie xóa người đăng nhập khi đóng trình duyệt
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    // Ép Cookie hết hạn sau một khoảng thời gian ngắn nếu không hoạt động
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    // Nếu SlidingExpiration = true, mỗi lần bạn click chuột thời gian sẽ được làm mới
    options.SlidingExpiration = true;

    // Cookie này sẽ bị xóa khi đóng trình duyệt (nếu không chọn Remember Me)
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký DbContext (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Connection1")));

// Đăng ký Identity (User + Role)
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Đăng ký Repository
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Đăng ký MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed role + admin user

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
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Auth}/{id?}");

app.Run();