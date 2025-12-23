using System;
using System.ComponentModel.DataAnnotations;

namespace StrateraPos.Models
{
    public enum UserRole
    {
        [Display(Name = "Administrator")]
        Admin,
        [Display(Name = "Manager")]
        Manager,
        [Display(Name = "Cashier")]
        Cashier
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        // Basic Info
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        // Authentication (store hashes, not plain passwords)
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;

        // Role & Permissions
        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "User Role")]
        public UserRole Role { get; set; } = UserRole.Cashier;

        // Account Flags & Audit
        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Date Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Login")]
        public DateTime? LastLoginAt { get; set; }

        [Display(Name = "Failed Login Attempts")]
        public int FailedLoginAttempts { get; set; } = 0;

        [Display(Name = "Account Locked Until")]
        public DateTime? LockedUntil { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime? UpdatedAt { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;

        // Computed Properties
        public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

        public string RoleDisplayName => Role switch
        {
            UserRole.Admin => "Administrator",
            UserRole.Manager => "Manager",
            UserRole.Cashier => "Cashier",
            _ => "Unknown"
        };

        public string StatusBadge => IsActive ? "Active" : "Inactive";
    }
}