using System.ComponentModel.DataAnnotations;
using ToYouEMS.ToYouEMS.Core.Models.Entities;

namespace ToYouEMS.ToYouEMS.Core.Models.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public UserType UserType { get; set; }
    }

    public class AuthResponse
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public UserType UserType { get; set; }
        public string Token { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class UserDTO
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public UserType UserType { get; set; }
        public bool IsActive { get; set; }
    }
}
