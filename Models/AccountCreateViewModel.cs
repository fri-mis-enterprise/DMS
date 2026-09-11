using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Document_Management.Models
{
    public sealed class AccountCreateViewModel
    {
        [Required(ErrorMessage = "Employee Number is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Employee Number must be a 4-digit number.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Employee Number must be a 4-digit number.")]
        [Display(Name = "Employee Number", Prompt = "Enter employee number")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "First Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in First Name.")]
        [Display(Name = "First Name", Prompt = "Enter first name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "Last Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in Last Name.")]
        [Display(Name = "Last Name", Prompt = "Enter last name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password", Prompt = "Enter your password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password", Prompt = "Confirm your password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; } = string.Empty;

        public string[] AccessDepartments { get; set; } = Array.Empty<string>();

        public string[] AccessCompanies { get; set; } = Array.Empty<string>();

        public bool IsActive { get; set; }

        public List<SelectListItem> Departments { get; set; } = [];

        public List<SelectListItem> Companies { get; set; } = [];
    }
}
