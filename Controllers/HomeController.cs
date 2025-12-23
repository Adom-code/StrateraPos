using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrateraPOS_System.Data;
using StrateraPOS_System.Models;
using StrateraPos.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StrateraPOS_System.Controllers
{
    [AnyAuthenticatedUser]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext db, ILogger<HomeController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var vm = new DashboardViewModel();

                // Get business settings for currency
                var settings = await _db.BusinessSettings.FirstOrDefaultAsync();
                if (settings != null)
                {
                    ViewBag.CurrencySymbol = settings.CurrencySymbol;
                    ViewBag.CurrencyCode = settings.CurrencyCode;
                    ViewBag.BusinessName = settings.BusinessName;
                }
                else
                {
                    ViewBag.CurrencySymbol = "₵";
                    ViewBag.CurrencyCode = "GHS";
                    ViewBag.BusinessName = "Stratera POS";
                }
                // Date ranges - FIXED (only declare once)
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var yesterday = today.AddDays(-1);
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var last30Days = today.AddDays(-30);

                // === ADD THIS DEBUG CODE ===
                _logger.LogInformation($"=== DASHBOARD DEBUG ===");
                _logger.LogInformation($"Today: {today}");
                _logger.LogInformation($"Tomorrow: {tomorrow}");
                _logger.LogInformation($"DateTime.Now: {DateTime.Now}");

                // Get all sales dates to see what's in the database
                var allSales = await _db.Sales.ToListAsync();
                _logger.LogInformation($"Total sales in database: {allSales.Count}");
                foreach (var sale in allSales.Take(5))
                {
                    _logger.LogInformation($"Sale Date: {sale.Date}, Receipt: {sale.ReceiptNumber}");
                }
                // === END DEBUG CODE ===

                // --- TODAY'S STATISTICS - FIXED ---
                var todaySales = await _db.Sales
                    .Where(s => s.Date >= today && s.Date < tomorrow)
                    .ToListAsync();

                _logger.LogInformation($"Found {todaySales.Count} sales for today"); // Add this too

                vm.TodaySalesCount = todaySales.Count;
                vm.TodayRevenue = todaySales.Sum(s => s.GrandTotal);

                // Yesterday's sales for comparison - FIXED
                var yesterdaySales = await _db.Sales
                    .Where(s => s.Date >= yesterday && s.Date < today)
                    .ToListAsync();

                vm.YesterdayRevenue = yesterdaySales.Sum(s => s.GrandTotal);

                // Calculate percentage change
                if (vm.YesterdayRevenue > 0)
                {
                    vm.RevenueChangePercent = ((vm.TodayRevenue - vm.YesterdayRevenue) / vm.YesterdayRevenue) * 100;
                }
                else
                {
                    vm.RevenueChangePercent = vm.TodayRevenue > 0 ? 100 : 0;
                }

                // --- THIS WEEK'S STATISTICS ---
                vm.WeekSalesCount = await _db.Sales
                    .Where(s => s.Date >= startOfWeek)
                    .CountAsync();

                vm.WeekRevenue = await _db.Sales
                    .Where(s => s.Date >= startOfWeek)
                    .SumAsync(s => (decimal?)s.GrandTotal) ?? 0m;

                // --- THIS MONTH'S STATISTICS ---
                vm.MonthSalesCount = await _db.Sales
                    .Where(s => s.Date >= startOfMonth)
                    .CountAsync();

                vm.MonthRevenue = await _db.Sales
                    .Where(s => s.Date >= startOfMonth)
                    .SumAsync(s => (decimal?)s.GrandTotal) ?? 0m;

                // --- OVERALL STATISTICS ---
                vm.TotalProducts = await _db.Products.CountAsync();
                vm.TotalSalesCount = await _db.Sales.CountAsync();
                vm.TotalSalesAmount = await _db.Sales
                    .SumAsync(s => (decimal?)s.GrandTotal) ?? 0m;

                // --- LOW STOCK ALERTS ---
                vm.LowStockCount = await _db.Products
                    .Where(p => p.IsActive && p.Stock <= p.LowStockThreshold)
                    .CountAsync();

                vm.LowStockProducts = await _db.Products
                    .Where(p => p.IsActive && p.Stock <= p.LowStockThreshold)
                    .OrderBy(p => p.Stock)
                    .Take(10)
                    .Select(p => new LowStockItem
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Stock = p.Stock,
                        LowStockThreshold = p.LowStockThreshold
                    })
                    .ToListAsync();

                // --- OUT OF STOCK ---
                vm.OutOfStockCount = await _db.Products
                    .Where(p => p.IsActive && p.Stock == 0)
                    .CountAsync();

                // --- RECENT SALES (Last 10) ---
                vm.RecentSales = await _db.Sales
                    .Include(s => s.Items)
                    .OrderByDescending(s => s.Date)
                    .Take(10)
                    .Select(s => new RecentSaleItem
                    {
                        Id = s.Id,
                        ReceiptNumber = s.ReceiptNumber,
                        Date = s.Date,
                        GrandTotal = s.GrandTotal,
                        ItemCount = s.Items.Count,
                        PaymentMethod = s.PaymentMethod
                    })
                    .ToListAsync();

                // --- SALES TREND (Last 7 Days) - FIXED ---
                vm.SalesTrendData = new List<SalesTrendItem>();
                for (int i = 6; i >= 0; i--)
                {
                    var date = today.AddDays(-i);
                    var nextDate = date.AddDays(1);
                    var dailySales = await _db.Sales
                        .Where(s => s.Date >= date && s.Date < nextDate)
                        .SumAsync(s => (decimal?)s.GrandTotal) ?? 0m;

                    vm.SalesTrendData.Add(new SalesTrendItem
                    {
                        Date = date.ToString("MMM dd"),
                        Amount = dailySales
                    });
                }

                // --- TOP 5 BEST SELLING PRODUCTS ---
                var topProductsData = await _db.Sales
                    .Where(s => s.Date >= last30Days)
                    .SelectMany(s => s.Items)
                    .ToListAsync(); // Load into memory first

                vm.TopProducts = topProductsData
                    .GroupBy(i => new { i.ProductId, i.ProductName })
                    .Select(g => new TopProductItem
                    {
                        ProductName = g.Key.ProductName,
                        TotalQuantity = g.Sum(i => i.Quantity),
                        TotalRevenue = g.Sum(i => i.Total) // Now this works because it's in memory
                    })
                    .OrderByDescending(p => p.TotalQuantity)
                    .Take(5)
                    .ToList();

                // --- RECENT ACTIVITY (Last 10) ---
                vm.RecentActivities = await _db.ActivityLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .Select(a => new ActivityItem
                    {
                        Id = a.Id,
                        Description = a.Description,
                        Timestamp = a.Timestamp,
                        ActivityType = a.ActivityType,
                        UserName = a.User != null ? a.User.FullName : "System"
                    })
                    .ToListAsync();

                // --- CATEGORIES COUNT ---
                vm.TotalCategories = await _db.Categories.CountAsync();

                // --- SUPPLIERS COUNT ---
                vm.TotalSuppliers = await _db.Suppliers.Where(s => s.IsActive).CountAsync();

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "An error occurred while loading the dashboard. Please try again.";
                return View(new DashboardViewModel());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}