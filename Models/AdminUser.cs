using System;
using System.ComponentModel.DataAnnotations;

namespace JobForFresher.Models
{
    public class AdminUser
    {
        public int Id { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutUntilUtc { get; set; }
        public string SecurityStamp { get; set; } = "";

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}