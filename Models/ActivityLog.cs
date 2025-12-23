using System;
namespace StrateraPos.Models
{
    public enum ActivityType
    {
        Login,
        Logout,
        CreateProduct,
        UpdateProduct,
        DeleteProduct,
        CreateCategory,
        UpdateCategory,
        DeleteCategory,
        CreateSale,
        UpdateSettings,
        CreateUser,
        UpdateUser,
        DeleteUser,
        Other
    }
    public class ActivityLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public ActivityType ActivityType { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        // Optional: Store related entity ID
        public string? EntityType { get; set; } // e.g., "Product", "Sale", "Category"
        public int? EntityId { get; set; }
        public string? IpAddress { get; set; }
    }
}