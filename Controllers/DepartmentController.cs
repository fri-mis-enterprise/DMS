using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly MasterDataService _masterDataService;
        private readonly ILogger<DepartmentController> _logger;
        private readonly string? _userRole;
        private readonly string? _userName;

        public DepartmentController(
            ApplicationDbContext dbContext,
            MasterDataService masterDataService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DepartmentController> logger)
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
                return View(await _dbContext.Departments.OrderBy(department => department.DepartmentName).ToListAsync(cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load departments.");
                TempData["ErrorMessage"] = "Failed to load departments.";
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
        public async Task<IActionResult> Create(DepartmentViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            OperationResult result;
            try
            {
                result = await _masterDataService.CreateDepartmentAsync(viewModel, _userName!, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create department {DepartmentName}.", viewModel.DepartmentName);
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Failed to create department.");
                return View(viewModel);
            }
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(viewModel);
            }

            TempData["success"] = "Department created successfully";
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
                var department = await _dbContext.Departments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (department == null)
                {
                    return NotFound();
                }

                return View(new DepartmentViewModel
                {
                    Id = department.Id,
                    DepartmentName = department.DepartmentName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load department {DepartmentId} for edit.", id);
                TempData["ErrorMessage"] = "Failed to load department.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DepartmentViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            OperationResult result;
            try
            {
                result = await _masterDataService.UpdateDepartmentAsync(viewModel, _userName!, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update department {DepartmentId}.", viewModel.Id);
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Failed to update department.");
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

            TempData["success"] = "Department updated successfully";
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
