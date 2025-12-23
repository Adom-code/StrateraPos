using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrateraPos.Models;
using StrateraPOS_System.Data;
using StrateraPOS_System.Models;

namespace StrateraPOS_System.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Products)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                TempData["Error"] = "An error occurred while loading categories. Please try again.";
                return View(new List<Category>());
            }
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if category name already exists
                    var exists = await _context.Categories
                        .AnyAsync(c => c.Name.ToLower() == category.Name.ToLower());

                    if (exists)
                    {
                        ModelState.AddModelError("Name", "A category with this name already exists.");
                        TempData["Error"] = "A category with this name already exists.";
                        return View(category);
                    }

                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();

                    // Log activity
                    var activityLog = new ActivityLog
                    {
                        UserId = 1, // TODO: Get from logged-in user
                        ActivityType = ActivityType.CreateCategory,
                        Description = $"Created category: {category.Name}",
                        EntityType = "Category",
                        EntityId = category.Id
                    };
                    _context.ActivityLogs.Add(activityLog);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Category created successfully: {CategoryName}", category.Name);
                    TempData["Success"] = $"Category '{category.Name}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating category: {CategoryName}", category.Name);
                    TempData["Error"] = "An error occurred while creating the category. Please try again.";
                    ModelState.AddModelError("", "Unable to save changes. Please try again.");
                }
            }
            else
            {
                TempData["Error"] = "Please correct the validation errors.";
            }

            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Category not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category for edit: {CategoryId}", id);
                TempData["Error"] = "An error occurred while loading the category.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
            {
                TempData["Error"] = "Invalid category data.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if another category with same name exists
                    var exists = await _context.Categories
                        .AnyAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != id);

                    if (exists)
                    {
                        ModelState.AddModelError("Name", "A category with this name already exists.");
                        TempData["Error"] = "A category with this name already exists.";
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    // Log activity
                    var activityLog = new ActivityLog
                    {
                        UserId = 1,
                        ActivityType = ActivityType.UpdateCategory,
                        Description = $"Updated category: {category.Name}",
                        EntityType = "Category",
                        EntityId = category.Id
                    };
                    _context.ActivityLogs.Add(activityLog);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Category updated successfully: {CategoryName}", category.Name);
                    TempData["Success"] = $"Category '{category.Name}' updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!await CategoryExistsAsync(category.Id))
                    {
                        _logger.LogWarning("Category not found during update: {CategoryId}", category.Id);
                        TempData["Error"] = "Category not found. It may have been deleted.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error updating category: {CategoryId}", category.Id);
                        TempData["Error"] = "The category was modified by another user. Please try again.";
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating category: {CategoryId}", category.Id);
                    TempData["Error"] = "An error occurred while updating the category. Please try again.";
                    ModelState.AddModelError("", "Unable to save changes. Please try again.");
                }
            }
            else
            {
                TempData["Error"] = "Please correct the validation errors.";
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return Json(new { success = false, message = "Category not found." });
                }

                // Check if category has products
                if (category.Products != null && category.Products.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Cannot delete category '{category.Name}' because it has {category.Products.Count} product(s). Please move or delete the products first."
                    });
                }

                var categoryName = category.Name;
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                // Log activity
                var activityLog = new ActivityLog
                {
                    UserId = 1,
                    ActivityType = ActivityType.DeleteCategory,
                    Description = $"Deleted category: {categoryName}",
                    EntityType = "Category",
                    EntityId = category.Id
                };
                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category deleted successfully: {CategoryName}", categoryName);
                return Json(new { success = true, message = $"Category '{categoryName}' deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the category. Please try again." });
            }
        }

        private async Task<bool> CategoryExistsAsync(int id)
        {
            return await _context.Categories.AnyAsync(e => e.Id == id);
        }
    }
}