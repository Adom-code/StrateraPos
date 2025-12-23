using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrateraPOS_System.Data;
using StrateraPos.Models;

namespace StrateraPos.Controllers
{
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuppliersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Suppliers
        public async Task<IActionResult> Index(string search, bool? isActive)
        {
            var suppliersQuery = _context.Suppliers
                .Include(s => s.Products)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                suppliersQuery = suppliersQuery.Where(s =>
                    s.Name.Contains(search) ||
                    (s.ContactPerson != null && s.ContactPerson.Contains(search)) ||
                    (s.Phone != null && s.Phone.Contains(search)) ||
                    (s.Email != null && s.Email.Contains(search)));
            }

            // Status filter
            if (isActive.HasValue)
            {
                suppliersQuery = suppliersQuery.Where(s => s.IsActive == isActive.Value);
            }

            var suppliers = await suppliersQuery
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.IsActive = isActive;

            return View(suppliers);
        }

        // GET: Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .Include(s => s.Products)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: Suppliers/Create
        public IActionResult Create()
        {
            return View(new Supplier());
        }

        // POST: Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                supplier.CreatedAt = DateTime.Now;
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Supplier created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        // POST: Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    supplier.UpdatedAt = DateTime.Now;
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Supplier updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // POST: Suppliers/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus([FromBody] int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return Json(new { success = false, message = "Supplier not found" });
            }

            supplier.IsActive = !supplier.IsActive;
            supplier.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Status updated successfully" });
        }

        // POST: Suppliers/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null)
            {
                return Json(new { success = false, message = "Supplier not found" });
            }

            // Check if supplier has products
            if (supplier.Products.Any())
            {
                return Json(new { success = false, message = "Cannot delete supplier with existing products. Please remove or reassign products first." });
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Supplier deleted successfully" });
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}