using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace StrateraPos.Filters
{
    /// <summary>
    /// Custom authorization attribute for session-based authentication
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeSessionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public AuthorizeSessionAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");
            var userRole = context.HttpContext.Session.GetString("UserRole");

            // Check if user is logged in
            if (string.IsNullOrEmpty(userId))
            {
                // Redirect to login with return URL
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            // Check role if roles are specified
            if (_allowedRoles != null && _allowedRoles.Length > 0)
            {
                if (string.IsNullOrEmpty(userRole) || !_allowedRoles.Contains(userRole))
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Shortcut attributes for common role checks
    /// </summary>
    public class AdminOnlyAttribute : AuthorizeSessionAttribute
    {
        public AdminOnlyAttribute() : base("Admin") { }
    }

    public class AdminOrManagerAttribute : AuthorizeSessionAttribute
    {
        public AdminOrManagerAttribute() : base("Admin", "Manager") { }
    }

    public class AnyAuthenticatedUserAttribute : AuthorizeSessionAttribute
    {
        public AnyAuthenticatedUserAttribute() : base() { }
    }
}