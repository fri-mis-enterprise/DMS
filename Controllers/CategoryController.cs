using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly MasterDataService _masterDataService;
        private readonly ILogger<CategoryController> _logger;
        private readonly string? _userRole;
        private readonly string? _userName;

        public CategoryController(
            ApplicationDbContext dbContext,
            MasterDataService masterDataService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CategoryController> logger)
        {
            _dbContext = dbContext;
            _masterDataService = masterDataService;
            _logger = logger;
            _userRole = httpContextAccessor.HttpContext?.Session.GetString("userRole")?.ToLowerInvariant();
            _userName = httpContextAccessor.HttpContext?.Session.GetString("username");
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            try
            {
                return View(await _dbContext.Categories.OrderBy(category => category.CategoryName).ToListAsync(cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load categories.");
                TempData["ErrorMessage"] = "Failed to load categories.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            var adminAccessResult = EnsureAdminAccess();
            return adminAccessResult ?? View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            OperationResult result;
            try
            {
                result = await _masterDataService.CreateCategoryAsync(viewModel, _userName!, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create category {CategoryName}.", viewModel.CategoryName);
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Failed to create category.");
                return View(viewModel);
            }
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(viewModel);
            }

            TempData["success"] = "Category created successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            try
            {
                var category = await _dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (category == null)
                {
                    return NotFound();
                }

                return View(new CategoryViewModel
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load category {CategoryId} for edit.", id);
                TempData["ErrorMessage"] = "Failed to load category.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            OperationResult result;
            try
            {
                result = await _masterDataService.UpdateCategoryAsync(viewModel, _userName!, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update category {CategoryId}.", viewModel.Id);
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Failed to update category.");
                return View(viewModel);
            }
            if (result.NotFound)
            {
                return NotFound();
            }

            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(viewModel);
            }

            TempData["success"] = "Category updated successfully";
            return RedirectToAction(nameof(Index));
        }

        private void AddErrors(OperationResult result)
        {
            ModelState.Clear();
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Key, error.Value);
            }

            if (result.GeneralError != null)
            {
                ModelState.AddModelError(string.Empty, result.GeneralError);
            }
        }

        private IActionResult? EnsureAdminAccess()
        {
            if (string.IsNullOrEmpty(_userName))
            {
                return RedirectToAction("Login", "Account");
            }

            if (_userRole != "admin")
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home");
            }

            return null;
        }
    }
}
