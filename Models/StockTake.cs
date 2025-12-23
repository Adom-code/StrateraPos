using StrateraPos.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrateraPOS_System.Models
{
    // Stock Take Session
    public class StockTake
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SessionNumber { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        public StockTakeStatus Status { get; set; } = StockTakeStatus.InProgress;

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public string? Notes { get; set; }

        // Calculated fields
        public int TotalItemsCounted { get; set; }
        public decimal TotalVarianceValue { get; set; }

        // Navigation
        public ICollection<StockTakeItem> Items { get; set; } = new List<StockTakeItem>();
    }

    // Individual product count in a stock take session
    public class StockTakeItem
    {
        [Key]
        public int Id { get; set; }

        public int StockTakeId { get; set; }
        [ForeignKey("StockTakeId")]
        public StockTake? StockTake { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public StrateraPos.Models.Product? Product { get; set; }

        [Required]
        public int SystemStock { get; set; } // What system shows

        [Required]
        public int PhysicalCount { get; set; } // What was actually counted

        [NotMapped]
        public int Variance => PhysicalCount - SystemStock; // Difference

        [NotMapped]
        public decimal VarianceValue => Variance * (Product?.Price ?? 0); // Money value

        public string? Reason { get; set; } // Why there's a difference

        public DateTime CountedDate { get; set; }
    }

    public enum StockTakeStatus
    {
        InProgress,
        Completed,
        Cancelled
    }

    public enum VarianceReason
    {
        Theft,
        Damage,
        Expired,
        CountingError,
        SystemError,
        Other
    }
}