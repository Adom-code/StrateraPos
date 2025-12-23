using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrateraPos.Models;
using StrateraPOS_System.Data;
using StrateraPOS_System.Models;

namespace StrateraPOS_System.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Products
        public async Task<IActionResult> Index(string? search, int? categoryId)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p =>
                        p.Name.Contains(search) ||
                        (p.Barcode != null && p.Barcode.Contains(search)));
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                var products = await query.OrderByDescending(p => p.Id).ToListAsync();

                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Search = search;
                ViewBag.CategoryId = categoryId;

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["Error"] = "An error occurred while loading products. Please try again.";
                return View(new List<Product>());
            }
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                var suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();

                ViewBag.Categories = categories;
                ViewBag.Suppliers = suppliers;

                if (categories == null || !categories.Any())
                {
                    TempData["Warning"] = "Please create at least one category before adding products.";
                    return RedirectToAction("Index", "Categories");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create product page");
                TempData["Error"] = "An error occurred. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // TEMPORARY DEBUG CODE - Log all ModelState errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                    .Select(x => new {
                        Field = x.Key,
                        Errors = x.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    })
                    .ToList();

                foreach (var error in errors)
                {
                    _logger.LogError("Validation Error - Field: {Field}, Messages: {Messages}",
                        error.Field, string.Join(", ", error.Errors));
                }

                TempData["Error"] = "Validation errors: " + string.Join("; ",
                    errors.Select(e => $"{e.Field}: {string.Join(", ", e.Errors)}"));
            }
            // END DEBUG CODE

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate barcode
                    if (!string.IsNullOrEmpty(product.Barcode))
                    {
                        var exists = await _context.Products
                            .AnyAsync(p => p.Barcode == product.Barcode);

                        if (exists)
                        {
                            ModelState.AddModelError("Barcode", "A product with this barcode already exists.");
                            TempData["Error"] = "A product with this barcode already exists.";
                            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                            ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
                            return View(product);
                        }
                    }

                    // Validate price logic
                    if (product.CostPrice > 0 && product.Price < product.CostPrice)
                    {
                        TempData["Warning"] = "Warning: Selling price is lower than cost price. This will result in a loss.";
                    }

                    // Validate expiry date
                    if (product.ExpiryDate.HasValue && product.ExpiryDate.Value < DateTime.Now)
                    {
                        TempData["Warning"] = "Warning: The product has already expired.";
                    }

                    product.IsActive = true;
                    product.CreatedAt = DateTime.Now;

                    _logger.LogInformation("About to add product: {ProductName}, Category: {CategoryId}",
                        product.Name, product.CategoryId);

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // Log activity
                    var activityLog = new ActivityLog
                    {
                        UserId = 1, // TODO: Get from logged-in user
                        ActivityType = ActivityType.CreateProduct,
                        Description = $"Added product: {product.Name}",
                        EntityType = "Product",
                        EntityId = product.Id
                    };
                    _context.ActivityLogs.Add(activityLog);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Product created successfully: {ProductName}", product.Name);
                    TempData["Success"] = $"Product '{product.Name}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product: {ProductName}", product.Name);
                    TempData["Error"] = $"An error occurred while creating the product: {ex.Message}";
                    ModelState.AddModelError("", "Unable to save changes. Please try again.");
                }
            }
            else
            {
                TempData["Error"] = "Please correct the validation errors.";
            }

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product for edit: {ProductId}", id);
                TempData["Error"] = "An error occurred while loading the product.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                TempData["Error"] = "Invalid product data.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate barcode (excluding current product)
                    if (!string.IsNullOrEmpty(product.Barcode))
                    {
                        var exists = await _context.Products
                            .AnyAsync(p => p.Barcode == product.Barcode && p.Id != id);

                        if (exists)
                        {
                            ModelState.AddModelError("Barcode", "Another product with this barcode already exists.");
                            TempData["Error"] = "Another product with this barcode already exists.";
                            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                            ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
                            return View(product);
                        }
                    }

                    // Validate price logic
                    if (product.CostPrice > 0 && product.Price < product.CostPrice)
                    {
                        TempData["Warning"] = "Warning: Selling price is lower than cost price. This will result in a loss.";
                    }

                    // Validate expiry date
                    if (product.ExpiryDate.HasValue && product.ExpiryDate.Value < DateTime.Now)
                    {
                        TempData["Warning"] = "Warning: The product has already expired.";
                    }

                    product.UpdatedAt = DateTime.Now;
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    // Log activity
                    var activityLog = new ActivityLog
                    {
                        UserId = 1,
                        ActivityType = ActivityType.UpdateProduct,
                        Description = $"Updated product: {product.Name}",
                        EntityType = "Product",
                        EntityId = product.Id
                    };
                    _context.ActivityLogs.Add(activityLog);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Product updated successfully: {ProductName}", product.Name);
                    TempData["Success"] = $"Product '{product.Name}' updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!await ProductExistsAsync(product.Id))
                    {
                        _logger.LogWarning("Product not found during update: {ProductId}", product.Id);
                        TempData["Error"] = "Product not found. It may have been deleted.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error updating product: {ProductId}", product.Id);
                        TempData["Error"] = "The product was modified by another user. Please try again.";
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {ProductId}", product.Id);
                    TempData["Error"] = "An error occurred while updating the product. Please try again.";
                    ModelState.AddModelError("", "Unable to save changes. Please try again.");
                }
            }
            else
            {
                TempData["Error"] = "Please correct the validation errors.";
            }

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                var productName = product.Name;
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                // Log activity
                var activityLog = new ActivityLog
                {
                    UserId = 1,
                    ActivityType = ActivityType.DeleteProduct,
                    Description = $"Deleted product: {productName}",
                    EntityType = "Product",
                    EntityId = product.Id
                };
                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product deleted successfully: {ProductName}", productName);
                return Json(new { success = true, message = $"Product '{productName}' deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the product. Please try again." });
            }
        }

        // POST: Products/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                product.IsActive = !product.IsActive;
                product.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var status = product.IsActive ? "activated" : "deactivated";
                _logger.LogInformation("Product {Status}: {ProductName}", status, product.Name);

                return Json(new { success = true, isActive = product.IsActive, message = $"Product '{product.Name}' {status} successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling product status: {ProductId}", id);
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        private async Task<bool> ProductExistsAsync(int id)
        {
            return await _context.Products.AnyAsync(e => e.Id == id);
        }
    }
}