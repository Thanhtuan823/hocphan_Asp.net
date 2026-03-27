using lab2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace lab2.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Các bảng chính
        public DbSet<Product> Products { get; set; }
        public DbSet<Categories> Category { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        // Khai báo để EF Core biết bảng này tồn tại trong Database

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Rất quan trọng: Phải gọi base trước để Identity hoạt động đúng
            base.OnModelCreating(modelBuilder);

            // Cấu hình kiểu dữ liệu tiền tệ cho SQL Server
            modelBuilder.Entity<OrderDetail>()
                .Property(o => o.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CartItem>()
                .Property(c => c.Price)
                .HasColumnType("decimal(18,2)");

            // Cấu hình quan hệ Order - OrderDetail (1 - nhiều)
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId);
            modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<string>(); // Lưu "Pending", "Completed" thay vì 0, 1
        }
    }
}