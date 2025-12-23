using System;

namespace StrateraPos.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Automatically calculates line total
        public decimal Total => Quantity * UnitPrice;
    }
}
