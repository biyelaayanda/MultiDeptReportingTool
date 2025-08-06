using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs
{
     public class LoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class RegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        public string Role { get; set; } = string.Empty;
        
        public int? DepartmentId { get; set; }
    }
}
