using lab2.Data;
using lab2.Models;
using lab2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Đăng ký SessionCart (giỏ hàng)
builder.Services.AddSession();
builder.Services.AddScoped<SessionCart>(sp => SessionCart.GetCart(sp));

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
        adminUser = new IdentityUser { UserName = "admin@lifeandtrees.com", Email = "admin@lifeandtrees.com" };
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