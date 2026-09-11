using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Document_Management.Models
{
    public class Account
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Prompt = "Enter employee ID")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Employee Number is required.")]
        [Display(Name = "Employee Number", Prompt = "Enter employee number")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Employee Number must be a 4-digit number.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Employee Number must be a 4-digit number.")]
        [Column(TypeName = "varchar(4)")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "First Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in First Name.")]
        [Display(Name = "First Name", Prompt = "Enter first name")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "Last Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in Last Name.")]
        [Display(Name = "Last Name", Prompt = "Enter last name")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Username is required.")]
        [Display(Prompt = "Enter username")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Prompt = "Enter your password")]
        public string Password { get; set; } = null!;

        [NotMapped]
        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password", Prompt = "Confirm your password")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = null!;

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; } = null!;

        public string AccessDepartments { get; set; } = null!;

        public string ModuleAccess { get; set; } = "DMS";

        public string AccessCompanies { get; set; } = null!;

        public bool IsActive { get; set; }

        #region Select List Properties

        [NotMapped]
        public List<SelectListItem>? Departments { get; set; }

        [NotMapped]
        public List<SelectListItem>? Companies { get; set; }

        #endregion Select List Properties
    }
}
