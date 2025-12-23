using System;

namespace StrateraPos.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        // Action (Login, Create Product, Delete Sale, etc.)
        public string ActionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
