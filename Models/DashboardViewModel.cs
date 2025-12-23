using System;
using System.Collections.Generic;
using StrateraPos.Models; // Add this using statement

namespace StrateraPOS_System.Models
{
    public class DashboardViewModel
    {
        // Today's Statistics
        public int TodaySalesCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal YesterdayRevenue { get; set; }
        public decimal RevenueChangePercent { get; set; }

        // Weekly Statistics
        public int WeekSalesCount { get; set; }
        public decimal WeekRevenue { get; set; }

        // Monthly Statistics
        public int MonthSalesCount { get; set; }
        public decimal MonthRevenue { get; set; }

        // Overall Statistics
        public int TotalProducts { get; set; }
        public int TotalSalesCount { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public int TotalCategories { get; set; }
        public int TotalSuppliers { get; set; }

        // Stock Alerts
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public List<LowStockItem> LowStockProducts { get; set; } = new List<LowStockItem>();

        // Recent Data
        public List<RecentSaleItem> RecentSales { get; set; } = new List<RecentSaleItem>();
        public List<ActivityItem> RecentActivities { get; set; } = new List<ActivityItem>();

        // Charts Data
        public List<SalesTrendItem> SalesTrendData { get; set; } = new List<SalesTrendItem>();
        public List<TopProductItem> TopProducts { get; set; } = new List<TopProductItem>();
    }

    public class RecentSaleItem
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal GrandTotal { get; set; }
        public int ItemCount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class LowStockItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Stock { get; set; }
        public int LowStockThreshold { get; set; }
    }

    public class SalesTrendItem
    {
        public string Date { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class TopProductItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ActivityItem
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public StrateraPos.Models.ActivityType ActivityType { get; set; } // CHANGED THIS LINE
        public string UserName { get; set; } = string.Empty;
    }
}