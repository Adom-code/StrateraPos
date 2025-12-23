using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace StrateraPos.Models
{
    public class Sale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Sale Date")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Subtotal is required")]
        [Range(0.01, 9999999.99, ErrorMessage = "Subtotal must be greater than 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Subtotal")]
        public decimal SubTotal { get; set; }

        [Range(0, 9999999.99, ErrorMessage = "Discount must be a positive number")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Discount")]
        public decimal Discount { get; set; }

        [Range(0, 9999999.99, ErrorMessage = "Tax must be a positive number")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tax")]
        public decimal Tax { get; set; }

        [Range(0, 9999999.99, ErrorMessage = "Service charge must be a positive number")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Service Charge")]
        public decimal ServiceCharge { get; set; }

        [Required(ErrorMessage = "Grand total is required")]
        [Range(0.01, 9999999.99, ErrorMessage = "Grand total must be greater than 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Grand Total")]
        public decimal GrandTotal { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(100, ErrorMessage = "Customer contact cannot exceed 100 characters")]
        [Display(Name = "Customer Contact")]
        public string? CustomerContact { get; set; }

        [Required]
        [Display(Name = "User ID")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required(ErrorMessage = "Receipt number is required")]
        [StringLength(50, ErrorMessage = "Receipt number cannot exceed 50 characters")]
        [Display(Name = "Receipt Number")]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Display(Name = "Receipt Sent")]
        public bool ReceiptSent { get; set; } = false;

        [StringLength(100, ErrorMessage = "Receipt sent to cannot exceed 100 characters")]
        [Display(Name = "Receipt Sent To")]
        public string? ReceiptSentTo { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public List<SaleItem> Items { get; set; } = new List<SaleItem>();

        // Computed Properties
        [NotMapped]
        [Display(Name = "Total Items")]
        public int TotalItems => Items?.Sum(i => i.Quantity) ?? 0;

        [NotMapped]
        [Display(Name = "Total Products")]
        public int TotalProducts => Items?.Count ?? 0;

        [NotMapped]
        [Display(Name = "Discount Percentage")]
        public decimal DiscountPercentage => SubTotal > 0 ? (Discount / SubTotal) * 100 : 0;

        [NotMapped]
        [Display(Name = "Amount After Discount")]
        public decimal AmountAfterDiscount => SubTotal - Discount;

        [NotMapped]
        [Display(Name = "Profit")]
        public decimal Profit
        {
            get
            {
                if (Items == null || !Items.Any()) return 0;
                // This would need product cost price to calculate accurately
                return 0; // Placeholder
            }
        }
    }
}