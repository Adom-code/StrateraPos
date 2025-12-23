using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrateraPos.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "Barcode cannot exceed 100 characters")]
        [Display(Name = "Barcode")]
        public string? Barcode { get; set; }

        [Required(ErrorMessage = "Selling price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Selling Price")]
        public decimal Price { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Cost price must be between 0 and 999,999.99")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Cost Price")]
        public decimal CostPrice { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be a positive number")]
        [Display(Name = "Stock Quantity")]
        public int Stock { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Low stock threshold must be a positive number")]
        [Display(Name = "Low Stock Alert Level")]
        public int LowStockThreshold { get; set; } = 10;

        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        [Display(Name = "Unit of Measure")]
        public string? Unit { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        // Foreign Keys
        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }  // ✅ Fixed: Changed from int? to int

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Display(Name = "Supplier")]
        public int? SupplierId { get; set; }  // Stays nullable (optional)

        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime? UpdatedAt { get; set; }

        // Computed Properties
        [NotMapped]
        [Display(Name = "Profit Margin")]
        public decimal ProfitMargin => Price > 0 && CostPrice > 0 ? ((Price - CostPrice) / Price) * 100 : 0;

        [NotMapped]
        [Display(Name = "Is Low Stock")]
        public bool IsLowStock => Stock <= LowStockThreshold;

        [NotMapped]
        [Display(Name = "Is Expired")]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;
    }
}