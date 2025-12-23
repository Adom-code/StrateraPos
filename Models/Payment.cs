using System;

namespace StrateraPos.Models
{
    public enum PaymentType
    {
        Cash,
        Card,
        MobileMoney
    }

    public class Payment
    {
        public int Id { get; set; }

        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        public PaymentType PaymentType { get; set; }

        public decimal Amount { get; set; }

        // For card/mobile money transactions
        public string? TransactionReference { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // Additional info
        public string? Notes { get; set; }
    }
}