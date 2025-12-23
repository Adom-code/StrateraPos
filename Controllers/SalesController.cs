using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrateraPos.Models;
using StrateraPOS_System.Data;
using StrateraPOS_System.Models;

namespace StrateraPOS_System.Controllers
{
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SalesController> _logger;

        public SalesController(ApplicationDbContext context, ILogger<SalesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Sales/Index - Main POS Interface
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.Products != null && c.Products.Any(p => p.IsActive && p.Stock > 0))
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                ViewBag.Categories = categories;

                var settings = await _context.BusinessSettings.FirstOrDefaultAsync();
                ViewBag.Settings = settings ?? new BusinessSettings
                {
                    BusinessName = "Stratera POS",
                    CurrencySymbol = "₵",
                    TaxPercentage = 0,
                    ServiceChargePercentage = 0
                };

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading POS interface");
                TempData["Error"] = "An error occurred while loading the POS interface. Please try again.";
                return View();
            }
        }

        // API: Search Products
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string? search, int? categoryId)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.Stock > 0);

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Trim();
                    query = query.Where(p =>
                        p.Name.Contains(search) ||
                        (p.Barcode != null && p.Barcode.Contains(search)));
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                var products = await query
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Price,
                        p.Stock,
                        p.Barcode,
                        Category = p.Category != null ? p.Category.Name : "Uncategorized"
                    })
                    .Take(50)
                    .ToListAsync();

                return Json(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return Json(new List<object>());
            }
        }

        // API: Get Product by ID
        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Where(p => p.Id == id && p.IsActive)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Price,
                        p.Stock,
                        p.Barcode
                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound(new { message = "Product not found or unavailable" });
                }

                return Json(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product: {ProductId}", id);
                return NotFound(new { message = "Error retrieving product" });
            }
        }

        // API: Process Sale
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSale([FromBody] SaleRequest request)
        {
            if (request?.Items == null || !request.Items.Any())
            {
                return BadRequest(new { success = false, message = "Cart is empty" });
            }

            // Validate request
            if (request.SubTotal <= 0 || request.GrandTotal <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid sale amounts" });
            }

            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                return BadRequest(new { success = false, message = "Payment method is required" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get business settings
                var settings = await _context.BusinessSettings.FirstOrDefaultAsync();

                // Validate all products and stock before processing
                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        return BadRequest(new { success = false, message = "Invalid quantity for product" });
                    }

                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        return BadRequest(new { success = false, message = $"Product with ID {item.ProductId} not found" });
                    }

                    if (!product.IsActive)
                    {
                        return BadRequest(new { success = false, message = $"Product '{product.Name}' is not available for sale" });
                    }

                    if (product.Stock < item.Quantity)
                    {
                        return BadRequest(new { success = false, message = $"Insufficient stock for '{product.Name}'. Available: {product.Stock}" });
                    }
                }

                // Create the sale
                var sale = new Sale
                {
                    Date = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    SubTotal = request.SubTotal,
                    Discount = request.Discount,
                    Tax = request.Tax,
                    ServiceCharge = request.ServiceCharge,
                    GrandTotal = request.GrandTotal,
                    PaymentMethod = request.PaymentMethod.Trim(),
                    CustomerContact = request.CustomerContact?.Trim() ?? string.Empty,
                    UserId = 1, // TODO: Get from current logged-in user
                    ReceiptNumber = GenerateReceiptNumber(),
                    Items = new List<SaleItem>()
                };

                // Process each sale item
                foreach (var item in request.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);

                    // Null check for product
                    if (product == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { success = false, message = $"Product with ID {item.ProductId} not found" });
                    }

                    // Reduce stock
                    product.Stock -= item.Quantity;
                    product.UpdatedAt = DateTime.Now;

                    // Add sale item
                    sale.Items.Add(new SaleItem
                    {
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                // Log activity
                var activityLog = new ActivityLog
                {
                    UserId = 1, // TODO: Get from current logged-in user
                    ActivityType = ActivityType.CreateSale,
                    Description = $"Processed sale #{sale.ReceiptNumber} - Total: {settings?.CurrencySymbol ?? "₵"}{sale.GrandTotal:N2} ({sale.Items.Count} items, {sale.TotalItems} units)",
                    EntityType = "Sale",
                    EntityId = sale.Id
                };
                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Sale processed successfully: {ReceiptNumber}", sale.ReceiptNumber);

                return Json(new
                {
                    success = true,
                    saleId = sale.Id,
                    receiptNumber = sale.ReceiptNumber,
                    message = "Sale processed successfully"
                });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error processing sale");
                return BadRequest(new { success = false, message = "A database error occurred. Please try again." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing sale");
                return BadRequest(new { success = false, message = $"Error processing sale: {ex.Message}" });
            }
        }

        // Generate unique receipt number
        private string GenerateReceiptNumber()
        {
            var date = DateTime.Now;
            var random = new Random().Next(1000, 9999);
            return $"RCP-{date:yyyyMMdd}-{random}";
        }

        // View receipt
        public async Task<IActionResult> Receipt(int id)
        {
            try
            {
                var sale = await _context.Sales
                    .Include(s => s.Items)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (sale == null)
                {
                    TempData["Error"] = "Receipt not found.";
                    return RedirectToAction(nameof(Index));
                }

                var settings = await _context.BusinessSettings.FirstOrDefaultAsync();
                ViewBag.Settings = settings ?? new BusinessSettings
                {
                    BusinessName = "Stratera POS",
                    CurrencySymbol = "₵"
                };

                return View(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading receipt: {SaleId}", id);
                TempData["Error"] = "An error occurred while loading the receipt.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Sales History
        public async Task<IActionResult> History(DateTime? startDate, DateTime? endDate, string? paymentMethod)
        {
            try
            {
                var query = _context.Sales
                    .Include(s => s.Items)
                    .Include(s => s.User)
                    .AsQueryable();

                // Date filters
                if (startDate.HasValue)
                {
                    query = query.Where(s => s.Date.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(s => s.Date.Date <= endDate.Value.Date);
                }

                // Payment method filter
                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query = query.Where(s => s.PaymentMethod == paymentMethod);
                }

                var sales = await query
                    .OrderByDescending(s => s.Date)
                    .ToListAsync();

                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.PaymentMethod = paymentMethod;

                var settings = await _context.BusinessSettings.FirstOrDefaultAsync();
                ViewBag.Settings = settings ?? new BusinessSettings { CurrencySymbol = "₵" };

                return View(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales history");
                TempData["Error"] = "An error occurred while loading sales history.";
                return View(new List<Sale>());
            }
        }
    }

    // Request model for processing sale
    public class SaleRequest
    {
        public List<SaleItemRequest> Items { get; set; } = new List<SaleItemRequest>();
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal GrandTotal { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? CustomerContact { get; set; }
    }

    public class SaleItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}