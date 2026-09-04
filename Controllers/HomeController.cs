using Document_Management.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Document_Management.Data;
using Document_Management.Utility.Constants;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        
        private readonly ApplicationDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        public async Task<IActionResult> Maintenance()
        {
            try
            {
                if (await _dbContext.AppSettings
                        .Where(s => s.SettingKey == AppSettingKey.MaintenanceMode)
                        .Select(s => s.Value == "true")
                        .FirstOrDefaultAsync())
                {
                    return View("Maintenance");
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load maintenance status.");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
