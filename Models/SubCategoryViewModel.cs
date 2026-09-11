using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Document_Management.Models
{
    public class SubCategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Sub-category name is required.")]
        [StringLength(100, ErrorMessage = "Sub-category name cannot exceed 100 characters.")]
        public string SubCategoryName { get; set; } = string.Empty;

        public List<SelectListItem>? Categories { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select an existing category.")]
        public int CategoryId { get; set; }
    }
}
