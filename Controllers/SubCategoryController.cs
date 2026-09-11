using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly MasterDataService _masterDataService;
        private readonly ILogger<SubCategoryController> _logger;
        private readonly string? _userRole;
        private readonly string? _userName;

        public SubCategoryController(
            ApplicationDbContext dbContext,
            MasterDataService masterDataService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SubCategoryController> logger)
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
                var subCategories = await _dbContext.SubCategories
                    .Include(subCategory => subCategory.Category)
                    .OrderBy(subCategory => subCategory.SubCategoryName)
                    .ToListAsync(cancellationToken);

                return View(subCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sub-categories.");
                TempData["ErrorMessage"] = "Failed to load sub-categories.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            try
            {
                return View(new SubCategoryViewModel
                {
                    Categories = await _masterDataService.GetCategoryOptionsAsync(cancellationToken)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sub-category create form.");
                TempData["ErrorMessage"] = "Failed to load sub-category form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoryViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            OperationResult result;
            try
            {
                viewModel.Categories = await _masterDataService.GetCategoryOptionsAsync(cancellationToken);
                result = await _masterDataService.CreateSubCategoryAsync(viewModel, _userName!, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create sub-category {SubCategoryName}.", viewModel.SubCategoryName);
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Failed to create sub-category.");
                return View(viewModel);
            }
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(viewModel);
            }

            TempData["success"] = "Sub-Category created successfully";
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
                var subCategory = await _dbContext.SubCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (subCategory == null)
                {
                    return NotFound();
                }

                return View(new SubCategoryViewModel
                {
                    Id = subCategory.Id,
                    SubCategoryName = subCategory.SubCategoryName,
                    CategoryId = subCategory.CategoryId,
                    Categories = await _masterDataService.GetCategoryOptionsAsync(cancellationToken)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sub-category {SubCategoryId} for edit.", id);
                TempData["ErrorMessage"] = "Failed to load sub-category.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubCategoryViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            OperationResult result;
            try
            {
                viewModel.Categories = await _masterDataService.GetCategoryOptionsAsync(cancellationToken);
                result = await _masterDataService.UpdateSubCategoryAsync(viewModel, _userName!, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update sub-category {SubCategoryId}.", viewModel.Id);
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Failed to update sub-category.");
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

            TempData["success"] = "Sub-Category updated successfully";
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
