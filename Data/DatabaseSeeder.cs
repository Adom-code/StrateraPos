using Microsoft.EntityFrameworkCore;
using StrateraPOS_System.Data;
using StrateraPos.Models;
using StrateraPos.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StrateraPOS_System.Data
{
    public static class DatabaseSeeder
    {
        /// <summary>
        /// Seeds the database with initial data including default admin user
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if admin user already exists
            if (await context.Users.AnyAsync(u => u.Username == "admin"))
            {
                return; // Admin already exists
            }

            // Create default admin user
            var (hash, salt) = PasswordHasher.HashPassword("admin123");

            var adminUser = new User
            {
                Username = "admin",
                FullName = "System Administrator",
                Email = "admin@straterapos.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);

            // Create a default cashier user
            var (cashierHash, cashierSalt) = PasswordHasher.HashPassword("cashier123");

            var cashierUser = new User
            {
                Username = "cashier",
                FullName = "Default Cashier",
                Email = "cashier@straterapos.com",
                PasswordHash = cashierHash,
                PasswordSalt = cashierSalt,
                Role = UserRole.Cashier,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(cashierUser);

            await context.SaveChangesAsync();

            Console.WriteLine("✅ Default users created:");
            Console.WriteLine("   Admin - Username: admin, Password: admin123");
            Console.WriteLine("   Cashier - Username: cashier, Password: cashier123");
            Console.WriteLine("   ⚠️  Please change these passwords after first login!");
        }
    }
}