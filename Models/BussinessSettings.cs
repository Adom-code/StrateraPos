using System.ComponentModel.DataAnnotations;

namespace StrateraPOS_System.Models
{
    public class BusinessSettings
    {
        public int Id { get; set; }

        [Required]
        public string BusinessName { get; set; } = "Stratera POS";

        public string LogoPath { get; set; } = "";
        public string Address { get; set; } = "";
        public string Contact { get; set; } = "";

        public decimal TaxPercentage { get; set; } = 0;
        public decimal ServiceChargePercentage { get; set; } = 0;

        [Required]
        public string CurrencyCode { get; set; } = "GHS";

        [Required]
        public string CurrencySymbol { get; set; } = "₵";
    }
}