using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrateraPos.Models
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Supplier name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Supplier name must be between 2 and 200 characters")]
        [Display(Name = "Supplier Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Contact person name cannot exceed 200 characters")]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        [Display(Name = "Physical Address")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "City name cannot exceed 100 characters")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "Country name cannot exceed 100 characters")]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        [StringLength(50, ErrorMessage = "Tax ID cannot exceed 50 characters")]
        [Display(Name = "Tax ID/TIN")]
        public string? TaxId { get; set; }

        [StringLength(30, ErrorMessage = "Payment terms cannot exceed 30 characters")]
        [Display(Name = "Payment Terms")]
        public string? PaymentTerms { get; set; }

        [Display(Name = "Credit Limit")]
        [Range(0, 9999999.99, ErrorMessage = "Credit limit must be between 0 and 9,999,999.99")]
        [DataType(DataType.Currency)]
        public decimal? CreditLimit { get; set; }

        [Display(Name = "Current Balance")]
        [DataType(DataType.Currency)]
        public decimal CurrentBalance { get; set; } = 0;

        [StringLength(100, ErrorMessage = "Bank name cannot exceed 100 characters")]
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [StringLength(50, ErrorMessage = "Account number cannot exceed 50 characters")]
        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Date Added")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<Product> Products { get; set; } = new List<Product>();

        // Computed Properties
        [NotMapped]
        [Display(Name = "Total Products")]
        public int TotalProducts => Products?.Count ?? 0;

        [NotMapped]
        [Display(Name = "Has Outstanding Balance")]
        public bool HasOutstandingBalance => CurrentBalance > 0;

        [NotMapped]
        [Display(Name = "Credit Available")]
        public decimal CreditAvailable => (CreditLimit ?? 0) - CurrentBalance;

        [NotMapped]
        [Display(Name = "Is Over Credit Limit")]
        public bool IsOverCreditLimit => CreditLimit.HasValue && CurrentBalance > CreditLimit.Value;
    }
}