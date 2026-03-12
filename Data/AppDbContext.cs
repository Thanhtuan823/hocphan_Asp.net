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
        public DbSet<Order> Orders { get; set; }          // Bảng đơn hàng
        public DbSet<OrderDetail> OrderDetails { get; set; } // Bảng chi tiết đơn hàng

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // rất quan trọng khi kế thừa IdentityDbContext

            // Không tạo bảng cho CartItem vì chỉ dùng trong session
            modelBuilder.Ignore<CartItem>();

            // Cấu hình kiểu dữ liệu cho Price (tránh cảnh báo truncate)
            modelBuilder.Entity<OrderDetail>()
                .Property(o => o.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)              // mỗi OrderDetail có 1 Order
                .WithMany(o => o.OrderDetails)       // mỗi Order có nhiều OrderDetail
                .HasForeignKey(od => od.OrderId);    // khóa ngoại là OrderId
        }
    }
}
