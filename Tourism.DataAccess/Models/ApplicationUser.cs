using Microsoft.AspNetCore.Identity;

namespace Tourism.DataAccess.Models
{
    // This class represents each user in the system
    public class ApplicationUser : IdentityUser
    {
        // Add extra profile fields if needed later
        public string FullName { get; set; } = string.Empty; // Initialize to avoid nullable warnings
    }
}
