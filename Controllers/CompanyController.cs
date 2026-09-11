using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly MasterDataService _masterDataService;
        private readonly ILogger<CompanyController> _logger;
        private readonly string? _userRole;
        private readonly string? _userName;

        public CompanyController(
            ApplicationDbContext dbContext,
            MasterDataService masterDataService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CompanyController> logger)
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
                return View(await _dbContext.Companies.OrderBy(company => company.CompanyName).ToListAsync(cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load companies.");
                TempData["ErrorMessage"] = "Failed to load companies.";
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
        public async Task<IActionResult> Create(CompanyViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            OperationResult result;
            try
            {
                result = await _masterDataService.CreateCompanyAsync(viewModel, _userName!, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create company {CompanyName}.", viewModel.CompanyName);
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Failed to create company.");
                return View(viewModel);
            }
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(viewModel);
            }

            TempData["success"] = "Company created successfully";
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
                var company = await _dbContext.Companies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (company == null)
                {
                    return NotFound();
                }

                return View(new CompanyViewModel
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load company {CompanyId} for edit.", id);
                TempData["ErrorMessage"] = "Failed to load company.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CompanyViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            OperationResult result;
            try
            {
                result = await _masterDataService.UpdateCompanyAsync(viewModel, _userName!, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update company {CompanyId}.", viewModel.Id);
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Failed to update company.");
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

            TempData["success"] = "Company updated successfully";
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
