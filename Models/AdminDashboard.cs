namespace lab2.Models
{
    public class AdminDashboard
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public string RawOrdersJson { get; set; }

    }
}
