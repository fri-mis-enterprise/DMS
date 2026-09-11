using System.ComponentModel.DataAnnotations;

namespace Document_Management.Models
{
    public sealed class AccountPasswordViewModel
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password", Prompt = "Enter a new password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm New Password", Prompt = "Confirm the new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
