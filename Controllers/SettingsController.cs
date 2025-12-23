using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrateraPOS_System.Data;
using StrateraPOS_System.Models;

namespace StrateraPOS_System.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SettingsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Settings
        public async Task<IActionResult> Index()
        {
            var settings = await _context.BusinessSettings.FirstOrDefaultAsync();

            // If no settings exist, create and save default
            if (settings == null)
            {
                settings = new BusinessSettings
                {
                    BusinessName = "Stratera POS",
                    CurrencyCode = "GHS",
                    CurrencySymbol = "₵",
                    TaxPercentage = 0,
                    ServiceChargePercentage = 0
                };

                _context.BusinessSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        // POST: Settings/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(BusinessSettings model, IFormFile? logoFile)
        {
            try
            {
                // Remove ModelState validation for LogoPath since it's optional
                ModelState.Remove("LogoPath");

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please fill in all required fields correctly.";
                    return View("Index", model);
                }

                // Handle logo upload if provided
                if (logoFile != null && logoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "logos");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(logoFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await logoFile.CopyToAsync(fileStream);
                    }

                    model.LogoPath = "/uploads/logos/" + uniqueFileName;
                }
                else
                {
                    // Keep existing logo if no new file uploaded
                    var existingSettings = await _context.BusinessSettings.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == model.Id);
                    if (existingSettings != null)
                    {
                        model.LogoPath = existingSettings.LogoPath;
                    }
                }

                // Check if this is an update or insert
                var existingRecord = await _context.BusinessSettings.FindAsync(model.Id);

                if (existingRecord != null)
                {
                    // Update existing record
                    existingRecord.BusinessName = model.BusinessName;
                    existingRecord.Address = model.Address;
                    existingRecord.Contact = model.Contact;
                    existingRecord.LogoPath = model.LogoPath;
                    existingRecord.TaxPercentage = model.TaxPercentage;
                    existingRecord.ServiceChargePercentage = model.ServiceChargePercentage;
                    existingRecord.CurrencyCode = model.CurrencyCode;
                    existingRecord.CurrencySymbol = model.CurrencySymbol;

                    _context.BusinessSettings.Update(existingRecord);
                }
                else
                {
                    // Insert new record
                    _context.BusinessSettings.Add(model);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Settings updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating settings: {ex.Message}";

                // Log the inner exception for debugging
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" - {ex.InnerException.Message}";
                }

                return View("Index", model);
            }
        }
    }
}