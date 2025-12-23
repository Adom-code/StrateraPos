using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrateraPOS_System.Data;
using StrateraPos.Models;
using StrateraPOS_System.Models;
using System.Text;

namespace StrateraPos.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Reports
        public async Task<IActionResult> Index(string reportType = "daily", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var today = DateTime.Today;

                // Set default date ranges based on report type
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    switch (reportType.ToLower())
                    {
                        case "daily":
                            startDate = today;
                            endDate = today;
                            break;
                        case "weekly":
                            startDate = today.AddDays(-(int)today.DayOfWeek);
                            endDate = startDate.Value.AddDays(6);
                            break;
                        case "monthly":
                            startDate = new DateTime(today.Year, today.Month, 1);
                            endDate = startDate.Value.AddMonths(1).AddDays(-1);
                            break;
                        case "yearly":
                            startDate = new DateTime(today.Year, 1, 1);
                            endDate = new DateTime(today.Year, 12, 31);
                            break;
                        case "custom":
                            startDate = today.AddDays(-30);
                            endDate = today;
                            break;
                    }
                }

                // Validate date range
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    TempData["Error"] = "Invalid date range.";
                    return View(new List<Sale>());
                }

                if (startDate.Value > endDate.Value)
                {
                    TempData["Error"] = "Start date cannot be after end date.";
                    var temp = startDate;
                    startDate = endDate;
                    endDate = temp;
                }

                // Get sales data
                var salesQuery = _context.Sales
                    .Include(s => s.Items)
                    .Include(s => s.User)
                    .Where(s => s.Date.Date >= startDate.Value.Date && s.Date.Date <= endDate.Value.Date);

                var sales = await salesQuery.OrderByDescending(s => s.Date).ToListAsync();

                // Calculate metrics
                var totalSales = sales.Sum(s => s.GrandTotal);
                var totalTransactions = sales.Count;
                var totalDiscount = sales.Sum(s => s.Discount);
                var totalTax = sales.Sum(s => s.Tax);
                var totalServiceCharge = sales.Sum(s => s.ServiceCharge);

                // Calculate cost and profit
                decimal totalCost = 0;
                foreach (var sale in sales)
                {
                    foreach (var item in sale.Items)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null && product.CostPrice > 0)
                        {
                            totalCost += product.CostPrice * item.Quantity;
                        }
                        else
                        {
                            // Estimate cost as 60% of selling price if cost price not set
                            totalCost += item.UnitPrice * 0.6m * item.Quantity;
                        }
                    }
                }

                var totalRevenue = sales.Sum(s => s.SubTotal);
                var totalProfit = totalRevenue - totalCost;
                var profitMargin = totalRevenue > 0 ? (totalProfit / totalRevenue * 100) : 0;

                // Get best-selling products
                var bestSellingProducts = sales
                    .SelectMany(s => s.Items)
                    .GroupBy(i => new { i.ProductId, i.ProductName })
                    .Select(g => new
                    {
                        ProductName = g.Key.ProductName,
                        TotalQuantity = g.Sum(i => i.Quantity),
                        TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice)
                    })
                    .OrderByDescending(p => p.TotalQuantity)
                    .Take(10)
                    .ToList();

                // Get payment method breakdown
                var paymentMethods = sales
                    .GroupBy(s => s.PaymentMethod)
                    .Select(g => new
                    {
                        Method = g.Key,
                        Count = g.Count(),
                        Total = g.Sum(s => s.GrandTotal)
                    })
                    .OrderByDescending(p => p.Total)
                    .ToList();
                // Get daily sales trend
                var dailySales = sales
                    .GroupBy(s => s.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"), // <-- ADD .ToString("yyyy-MM-dd")
                        TotalSales = g.Sum(s => s.GrandTotal),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // Get stock status
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => p.IsActive)
                    .ToListAsync();

                var lowStockProducts = products.Where(p => p.Stock > 0 && p.Stock <= p.LowStockThreshold).ToList();
                var outOfStockProducts = products.Where(p => p.Stock == 0).ToList();
                var totalProducts = products.Count;
                var totalStockValue = products.Sum(p => p.Price * p.Stock);

                // Get business settings
                var settings = await _context.BusinessSettings.FirstOrDefaultAsync();

                // Pass data to view
                ViewBag.ReportType = reportType;
                ViewBag.StartDate = startDate.Value;
                ViewBag.EndDate = endDate.Value;
                ViewBag.TotalSales = totalSales;
                ViewBag.TotalTransactions = totalTransactions;
                ViewBag.TotalDiscount = totalDiscount;
                ViewBag.TotalTax = totalTax;
                ViewBag.TotalServiceCharge = totalServiceCharge;
                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.TotalCost = totalCost;
                ViewBag.TotalProfit = totalProfit;
                ViewBag.ProfitMargin = profitMargin;
                ViewBag.BestSellingProducts = bestSellingProducts;
                ViewBag.PaymentMethods = paymentMethods;
                ViewBag.DailySales = dailySales;
                ViewBag.LowStockProducts = lowStockProducts;
                ViewBag.OutOfStockProducts = outOfStockProducts;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.TotalStockValue = totalStockValue;
                ViewBag.Settings = settings ?? new BusinessSettings { CurrencySymbol = "₵" };

                return View(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports");
                TempData["Error"] = "An error occurred while loading reports. Please try again.";
                ViewBag.Settings = new BusinessSettings { CurrencySymbol = "₵" };
                return View(new List<Sale>());
            }
        }

        // Export Sales Report to CSV
        [HttpGet]
        public async Task<IActionResult> ExportSalesCSV(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    startDate = DateTime.Today.AddDays(-30);
                    endDate = DateTime.Today;
                }

                var sales = await _context.Sales
                    .Include(s => s.Items)
                    .Include(s => s.User)
                    .Where(s => s.Date.Date >= startDate.Value.Date && s.Date.Date <= endDate.Value.Date)
                    .OrderByDescending(s => s.Date)
                    .ToListAsync();

                var csv = new StringBuilder();
                csv.AppendLine("Receipt Number,Date,Time,Customer Contact,Items Count,Items Quantity,Subtotal,Discount,Tax,Service Charge,Grand Total,Payment Method,Cashier");

                foreach (var sale in sales)
                {
                    var itemsCount = sale.Items.Count;
                    var itemsQuantity = sale.Items.Sum(i => i.Quantity);
                    var cashier = sale.User?.Username ?? "Admin";
                    var customerContact = string.IsNullOrEmpty(sale.CustomerContact) ? "Walk-in" : sale.CustomerContact;

                    csv.AppendLine($"\"{sale.ReceiptNumber}\",\"{sale.Date:yyyy-MM-dd}\",\"{sale.Date:HH:mm:ss}\",\"{customerContact}\",{itemsCount},{itemsQuantity},{sale.SubTotal:F2},{sale.Discount:F2},{sale.Tax:F2},{sale.ServiceCharge:F2},{sale.GrandTotal:F2},\"{sale.PaymentMethod}\",\"{cashier}\"");
                }

                var fileName = $"Sales_Report_{startDate.Value:yyyyMMdd}_{endDate.Value:yyyyMMdd}.csv";
                var bytes = Encoding.UTF8.GetBytes(csv.ToString());

                _logger.LogInformation("Exported sales CSV: {FileName}", fileName);
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales CSV");
                TempData["Error"] = "An error occurred while exporting sales data.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Export Inventory Report to CSV
        [HttpGet]
        public async Task<IActionResult> ExportInventoryCSV()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var csv = new StringBuilder();
                csv.AppendLine("ID,Name,Barcode,Category,Supplier,Cost Price,Selling Price,Stock,Low Stock Threshold,Stock Value,Profit Margin,Status");

                foreach (var product in products)
                {
                    var category = product.Category?.Name ?? "Uncategorized";
                    var supplier = product.Supplier?.Name ?? "N/A";
                    var barcode = string.IsNullOrEmpty(product.Barcode) ? "N/A" : product.Barcode;
                    var stockValue = product.Price * product.Stock;
                    var profitMargin = product.Price > 0 && product.CostPrice > 0
                        ? ((product.Price - product.CostPrice) / product.Price * 100)
                        : 0;
                    var status = product.Stock == 0 ? "Out of Stock"
                        : product.Stock <= product.LowStockThreshold ? "Low Stock"
                        : "In Stock";

                    csv.AppendLine($"{product.Id},\"{product.Name}\",\"{barcode}\",\"{category}\",\"{supplier}\",{product.CostPrice:F2},{product.Price:F2},{product.Stock},{product.LowStockThreshold},{stockValue:F2},{profitMargin:F2},\"{status}\"");
                }

                var fileName = $"Inventory_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = Encoding.UTF8.GetBytes(csv.ToString());

                _logger.LogInformation("Exported inventory CSV: {FileName}", fileName);
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory CSV");
                TempData["Error"] = "An error occurred while exporting inventory data.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Stock Report
        public async Task<IActionResult> StockReport(string filter = "all")
        {
            try
            {
                var productsQuery = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => p.IsActive);

                switch (filter.ToLower())
                {
                    case "lowstock":
                        productsQuery = productsQuery.Where(p => p.Stock > 0 && p.Stock <= p.LowStockThreshold);
                        break;
                    case "outofstock":
                        productsQuery = productsQuery.Where(p => p.Stock == 0);
                        break;
                    case "instock":
                        productsQuery = productsQuery.Where(p => p.Stock > p.LowStockThreshold);
                        break;
                }

                var products = await productsQuery.OrderBy(p => p.Stock).ThenBy(p => p.Name).ToListAsync();

                ViewBag.Filter = filter;
                ViewBag.TotalValue = products.Sum(p => p.Price * p.Stock);

                var settings = await _context.BusinessSettings.FirstOrDefaultAsync();
                ViewBag.Settings = settings ?? new BusinessSettings { CurrencySymbol = "₵" };

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stock report");
                TempData["Error"] = "An error occurred while loading stock report.";
                ViewBag.Settings = new BusinessSettings { CurrencySymbol = "₵" };
                return View(new List<Product>());
            }
        }

        // Sales Summary (for dashboard or quick view)
        public async Task<IActionResult> SalesSummary()
        {
            try
            {
                var today = DateTime.Today;
                var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
                var thisMonthStart = new DateTime(today.Year, today.Month, 1);

                var todaySales = await _context.Sales
                    .Where(s => s.Date.Date == today)
                    .SumAsync(s => (decimal?)s.GrandTotal) ?? 0;

                var weekSales = await _context.Sales
                    .Where(s => s.Date.Date >= thisWeekStart)
                    .SumAsync(s => (decimal?)s.GrandTotal) ?? 0;

                var monthSales = await _context.Sales
                    .Where(s => s.Date.Date >= thisMonthStart)
                    .SumAsync(s => (decimal?)s.GrandTotal) ?? 0;

                var yearSales = await _context.Sales
                    .Where(s => s.Date.Year == today.Year)
                    .SumAsync(s => (decimal?)s.GrandTotal) ?? 0;

                ViewBag.TodaySales = todaySales;
                ViewBag.WeekSales = weekSales;
                ViewBag.MonthSales = monthSales;
                ViewBag.YearSales = yearSales;

                var settings = await _context.BusinessSettings.FirstOrDefaultAsync();
                ViewBag.Settings = settings ?? new BusinessSettings { CurrencySymbol = "₵" };

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales summary");
                TempData["Error"] = "An error occurred while loading sales summary.";
                ViewBag.Settings = new BusinessSettings { CurrencySymbol = "₵" };
                return View();
            }
        }

        // Product Performance Report
        public async Task<IActionResult> ProductPerformance(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue) startDate = DateTime.Today.AddDays(-30);
                if (!endDate.HasValue) endDate = DateTime.Today;

                var salesItems = await _context.Sales
                    .Where(s => s.Date.Date >= startDate.Value.Date && s.Date.Date <= endDate.Value.Date)
                    .SelectMany(s => s.Items)
                    .GroupBy(i => new { i.ProductId, i.ProductName })
                    .Select(g => new
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName,
                        TotalQuantitySold = g.Sum(i => i.Quantity),
                        TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice),
                        TransactionCount = g.Count()
                    })
                    .OrderByDescending(p => p.TotalRevenue)
                    .ToListAsync();

                ViewBag.StartDate = startDate.Value;
                ViewBag.EndDate = endDate.Value;
                ViewBag.ProductPerformance = salesItems;

                var settings = await _context.BusinessSettings.FirstOrDefaultAsync();
                ViewBag.Settings = settings ?? new BusinessSettings { CurrencySymbol = "₵" };

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product performance report");
                TempData["Error"] = "An error occurred while loading product performance report.";
                ViewBag.Settings = new BusinessSettings { CurrencySymbol = "₵" };
                return View();
            }
        }
    }
}