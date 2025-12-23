using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrateraPOS_System.Data;
using StrateraPos.Models;
using StrateraPos.Services;
using StrateraPos.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StrateraPos.Controllers
{
    [AdminOnly] // Only admins can manage users
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Password is required.");
                return View(user);
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Password and confirmation do not match.");
                return View(user);
            }

            if (password.Length < 6)
            {
                ModelState.AddModelError("", "Password must be at least 6 characters long.");
                return View(user);
            }

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                ModelState.AddModelError("", "Username already exists.");
                return View(user);
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("", "Email already exists.");
                return View(user);
            }

            // Hash password
            var (hash, salt) = PasswordHasher.HashPassword(password);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;
            user.IsDeleted = false;

            _context.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User '{user.FullName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null || existingUser.IsDeleted)
            {
                return NotFound();
            }

            // Check if username is taken by another user
            if (await _context.Users.AnyAsync(u => u.Username == user.Username && u.Id != id))
            {
                ModelState.AddModelError("", "Username already exists.");
                return View(user);
            }

            // Check if email is taken by another user
            if (await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != id))
            {
                ModelState.AddModelError("", "Email already exists.");
                return View(user);
            }

            // Update user properties
            existingUser.Username = user.Username;
            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;
            existingUser.IsActive = user.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"User '{user.FullName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Soft delete - don't actually remove from database
                user.IsDeleted = true;
                user.IsActive = false;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"User '{user.FullName}' deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Users/ResetPassword/5
        public async Task<IActionResult> ResetPassword(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword, string confirmPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "Password is required.";
                return View(user);
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Password and confirmation do not match.";
                return View(user);
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters long.";
                return View(user);
            }

            // Hash new password
            var (hash, salt) = PasswordHasher.HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Password reset successfully for '{user.FullName}'!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User '{user.FullName}' is now {(user.IsActive ? "active" : "inactive")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}