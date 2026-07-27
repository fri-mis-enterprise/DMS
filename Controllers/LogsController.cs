using Document_Management.Models;
using Document_Management.Service;
using Microsoft.AspNetCore.Mvc;

namespace Document_Management.Controllers
{
    public class LogsController : Controller
    {
        private readonly ILogQueryService _logQueryService;

        public LogsController(ILogQueryService logQueryService)
        {
            _logQueryService = logQueryService;
        }

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetActivityLogs([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _logQueryService.GetActivityLogsAsync(parameters, cancellationToken);

                return Json(new
                {
                    draw = result.Draw,
                    recordsTotal = result.RecordsTotal,
                    recordsFiltered = result.RecordsFiltered,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
