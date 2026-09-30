using Document_Management.Models;
using Document_Management.Repository;
using Document_Management.Service;
using Microsoft.AspNetCore.Mvc;

namespace Document_Management.Controllers
{
    public class ReportController : Controller
    {
        private readonly ReportRepo _reportRepo;
        private readonly ReportExcelExportService _reportExcelExportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            ReportRepo reportRepo,
            ReportExcelExportService reportExcelExportService,
            ILogger<ReportController> logger)
        {
            _reportRepo = reportRepo;
            _reportExcelExportService = reportExcelExportService;
            _logger = logger;
        }

        // Display the form
        public IActionResult ActivityReportForm()
        {
            return View(new ActivityReportViewModel());
        }

        // Generate the report
        public async Task<IActionResult> GenerateFileUploadReport(DateOnly dateFrom, DateOnly dateTo)
        {
            try
            {
                if (dateFrom > dateTo)
                {
                    TempData["error"] = "Date from cannot be greater than Date to date.";
                    return RedirectToAction(nameof(ActivityReportForm));
                }

                var uploadedFiles = await _reportRepo.GenerateUploadedFiles(dateFrom, dateTo);

                var model = new ActivityReportViewModel
                {
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    UploadedFiles = uploadedFiles,
                    CurrentUser = HttpContext.Session.GetString("username") ?? string.Empty
                };

                return View("FileUploadReport", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate file upload report from {DateFrom} to {DateTo}.", dateFrom, dateTo);
                TempData["ErrorMessage"] = "Failed to generate report.";
                return RedirectToAction(nameof(ActivityReportForm));
            }
        }

        public async Task<IActionResult> ExportFileUploadReport(
            DateOnly dateFrom,
            DateOnly dateTo,
            CancellationToken cancellationToken)
        {
            try
            {
                if (dateFrom > dateTo)
                {
                    TempData["error"] = "Date from cannot be greater than Date to date.";
                    return RedirectToAction(nameof(GenerateFileUploadReport), new { dateTo, dateFrom });
                }

                var uploadedFiles = await _reportRepo.GenerateUploadedFiles(dateFrom, dateTo, cancellationToken);
                var currentUser = HttpContext.Session.GetString("username") ?? string.Empty;
                var fileContents = _reportExcelExportService.GenerateFileUploadReport(
                    dateFrom,
                    dateTo,
                    currentUser,
                    uploadedFiles);
                var fileName = $"FileUploadReport_{dateFrom:yyyyMMdd}_{dateTo:yyyyMMdd}.xlsx";

                return File(
                    fileContents,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export file upload report from {DateFrom} to {DateTo}", dateFrom, dateTo);
                TempData["error"] = "Failed to export report.";
                return RedirectToAction(nameof(GenerateFileUploadReport), new { dateTo, dateFrom });
            }
        }
    }
}
