using Microsoft.EntityFrameworkCore;
using StrateraPos.Models;
using StrateraPOS_System.Models;

namespace StrateraPOS_System.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Existing DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<BusinessSettings> BusinessSettings { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Payment> Payments { get; set; }

        // NEW: Stock Take DbSets
        public DbSet<StockTake> StockTakes { get; set; }
        public DbSet<StockTakeItem> StockTakeItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision for monetary values
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.CostPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.SubTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.Discount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.Tax)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.ServiceCharge)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.GrandTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleItem>()
                .Property(si => si.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<StockTake>()
                .Property(st => st.TotalVarianceValue)
                .HasPrecision(18, 2);

            // Seed default business settings
            modelBuilder.Entity<BusinessSettings>().HasData(
                new BusinessSettings
                {
                    Id = 1,
                    BusinessName = "Stratera POS",
                    CurrencySymbol = "₵",
                    CurrencyCode = "GHS",
                    TaxPercentage = 12.5m,
                    ServiceChargePercentage = 0m,
                    Address = "",
                    Contact = "",
                    LogoPath = ""
                }
            );
        }
    }
}