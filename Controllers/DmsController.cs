using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Repository;
using Document_Management.Service;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Document_Management.Controllers
{
    public class DmsController : Controller
    {
        private readonly UserRepo _userRepo;
        private readonly ILogger<DmsController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICloudStorageService _cloudStorage;
        private readonly IDmsAccessService _accessService;
        private readonly IDocumentStorageWorkflowService _documentStorageWorkflowService;
        private readonly IPdfUploadValidationService _pdfUploadValidationService;
        private readonly IDmsQueryService _dmsQueryService;
        private readonly IDmsSearchService _dmsSearchService;

        public DmsController(
            ApplicationDbContext context,
            UserRepo userRepo,
            ILogger<DmsController> logger,
            ICloudStorageService cloudStorage,
            IDmsAccessService accessService,
            IDocumentStorageWorkflowService documentStorageWorkflowService,
            IPdfUploadValidationService pdfUploadValidationService,
            IDmsQueryService dmsQueryService,
            IDmsSearchService dmsSearchService)
        {
            _dbContext = context;
            _userRepo = userRepo;
            _logger = logger;
            _cloudStorage = cloudStorage;
            _accessService = accessService;
            _documentStorageWorkflowService = documentStorageWorkflowService;
            _pdfUploadValidationService = pdfUploadValidationService;
            _dmsQueryService = dmsQueryService;
            _dmsSearchService = dmsSearchService;
        }

        private IActionResult? CheckDepartmentAccess(string department)
        {
            if (!_accessService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            if (_accessService.CanAccessDepartment(department))
            {
                return null;
            }

            TempData["ErrorMessage"] = $"You have no access to {department.Replace("_", " ")}. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        private IActionResult? CheckCompanyAccess(string company)
        {
            if (!_accessService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            if (_accessService.CanAccessCompany(company))
            {
                return null;
            }

            TempData["ErrorMessage"] = $"You have no access to {company.Replace("_", " ")}. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        private IActionResult? EnsureUploadAccess()
        {
            if (!_accessService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            if (_accessService.CanUpload())
            {
                return null;
            }

            TempData["ErrorMessage"] = "You have no access to upload a file. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        private IActionResult? EnsureDocumentMutationAccess(FileDocument fileDocument)
        {
            if (!_accessService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            if (_accessService.CanMutate(fileDocument))
            {
                return null;
            }

            TempData["ErrorMessage"] = "You have no access to modify this file. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        private async Task<FileDocument> GetModelSelectList(FileDocument model, CancellationToken cancellationToken)
        {
            model.Companies = await _dbContext
                .Companies
                .OrderBy(x => x.CompanyName)
                .Select(x => new SelectListItem { Text = x.CompanyName, Value = x.CompanyName })
                .ToListAsync(cancellationToken);

            var currentYear = DateTimeHelper.GetCurrentPhilippineTime().Year;
            var yearsList = new List<SelectListItem>();

            for (var i = currentYear - 20; i <= currentYear + 10; i++)
            {
                yearsList.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString(),
                    Selected = i.ToString() == model.Year
                });
            }

            model.Years = yearsList;

            model.Departments = await _dbContext
                .Departments
                .OrderBy(x => x.DepartmentName)
                .Select(x => new SelectListItem { Text = x.DepartmentName, Value = x.DepartmentName })
                .ToListAsync(cancellationToken);

            model.Categories = await _dbContext
                .Categories
                .OrderBy(x => x.CategoryName)
                .Select(x => new SelectListItem { Text = x.CategoryName, Value = x.CategoryName })
                .ToListAsync(cancellationToken);

            return model;
        }

        [HttpGet]
        public async Task<IActionResult> UploadFile(CancellationToken cancellationToken)
        {
            var uploadAccessResult = EnsureUploadAccess();
            if (uploadAccessResult != null)
            {
                return uploadAccessResult;
            }

            try
            {
                var model = await GetModelSelectList(new FileDocument(), cancellationToken);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load upload form.");
                TempData["error"] = "Failed to load upload form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(
            FileDocument fileDocument,
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            var uploadAccessResult = EnsureUploadAccess();
            if (uploadAccessResult != null)
            {
                return uploadAccessResult;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            fileDocument = await GetModelSelectList(fileDocument, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (file == null || file.Length == 0)
                {
                    ModelState.AddModelError("file", "Please select a PDF file to upload.");
                }

                if (string.IsNullOrEmpty(fileDocument.BoxNumber))
                {
                    ModelState.AddModelError(nameof(FileDocument.BoxNumber), "Box Number is required.");
                }

                if (string.IsNullOrEmpty(fileDocument.SubmittedBy))
                {
                    ModelState.AddModelError(nameof(FileDocument.SubmittedBy), "Submitted By is required.");
                }

                if (fileDocument.DateSubmitted == null)
                {
                    ModelState.AddModelError(nameof(FileDocument.DateSubmitted), "Date Submitted is required.");
                }

                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Please fill out all the required data.";
                    return View(fileDocument);
                }

                var uploadedFile = file!;

                var pdfValidationResult = await _pdfUploadValidationService.ValidateAsync(uploadedFile, cancellationToken);
                if (!pdfValidationResult.IsValid)
                {
                    TempData["error"] = pdfValidationResult.ErrorMessage;
                    return View(fileDocument);
                }

                if (await _userRepo.CheckIfFileExists(uploadedFile.FileName, cancellationToken))
                {
                    TempData["error"] = "This file already exists in our database!";
                    return View(fileDocument);
                }

                var uploadResult = await _documentStorageWorkflowService.UploadAsync(fileDocument, uploadedFile, cancellationToken);

                fileDocument.DateUploaded = uploadResult.UploadedAt;
                fileDocument.Name = uploadResult.StoredFileName;
                fileDocument.Location = uploadResult.ObjectName;
                fileDocument.FileSize = uploadResult.FileSize;
                fileDocument.Username = _accessService.Username!;
                fileDocument.OriginalFilename = uploadedFile.FileName;
                fileDocument.NumberOfPages = pdfValidationResult.PageCount;
                fileDocument.IsInCloudStorage = true;
                fileDocument.ExtractedText = string.Empty;
                fileDocument.OcrStatus = OcrStatuses.Pending;
                fileDocument.OcrQueuedAt = DateTimeHelper.GetCurrentPhilippineTime();
                fileDocument.OcrStartedAt = null;
                fileDocument.OcrCompletedAt = null;
                fileDocument.OcrAttemptCount = 0;
                fileDocument.OcrError = string.Empty;

                await _dbContext.FileDocuments.AddAsync(fileDocument, cancellationToken);

                stopwatch.Stop();
                var duration = stopwatch.Elapsed;
                var fileSizeInMb = uploadedFile.Length / (1024.0 * 1024.0);

                var logs = new LogsModel(
                    _accessService.Username!,
                    $"Upload {uploadedFile.FileName} to Cloud Storage in {uploadResult.FolderPath} {fileDocument.NumberOfPages} page(s). " +
                    $"Size: {fileSizeInMb:F2} MB. Duration: {duration.TotalSeconds:F2} seconds."
                );
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "File uploaded successfully. Search text may take a while to appear while text extraction completes.";

                return View(fileDocument);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred during file upload to Cloud Storage.");
                TempData["error"] = "Contact MIS: An error occurred during file upload.";
            }

            return View(fileDocument);
        }

        public async Task<IActionResult> DownloadFile(CancellationToken cancellationToken)
        {
            if (!_accessService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var companies = await _dmsQueryService.GetAccessibleCompaniesAsync(cancellationToken);
                return View(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load accessible companies.");
                TempData["error"] = "Failed to load file browser.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> CompanyFolder(string folderName, CancellationToken cancellationToken)
        {
            ViewBag.CompanyFolder = folderName;

            var companyAccessResult = CheckCompanyAccess(folderName);
            if (companyAccessResult != null)
            {
                return companyAccessResult;
            }

            try
            {
                var years = await _dmsQueryService.GetYearsAsync(folderName, cancellationToken);
                return View(years);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load years for company {Company}.", folderName);
                TempData["error"] = "Failed to load company folder.";
                return RedirectToAction(nameof(DownloadFile));
            }
        }

        public async Task<IActionResult> YearFolder(string companyFolderName, string yearFolderName, CancellationToken cancellationToken)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;

            var companyAccessResult = CheckCompanyAccess(companyFolderName);
            if (companyAccessResult != null)
            {
                return companyAccessResult;
            }

            try
            {
                var departments = await _dmsQueryService.GetAccessibleDepartmentsAsync(companyFolderName, yearFolderName, cancellationToken);
                return View(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load departments for company {Company} and year {Year}.", companyFolderName, yearFolderName);
                TempData["error"] = "Failed to load year folder.";
                return RedirectToAction(nameof(CompanyFolder), new { folderName = companyFolderName });
            }
        }

        public async Task<IActionResult> DepartmentFolder(string departmentFolderName, string companyFolderName, string yearFolderName, CancellationToken cancellationToken)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;

            var departmentAccessResult = CheckDepartmentAccess(departmentFolderName);
            if (departmentAccessResult != null)
            {
                return departmentAccessResult;
            }

            try
            {
                var categories = await _dmsQueryService.GetCategoriesAsync(companyFolderName, yearFolderName, departmentFolderName, cancellationToken);
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load categories for department {Department}.", departmentFolderName);
                TempData["error"] = "Failed to load department folder.";
                return RedirectToAction(nameof(YearFolder), new { companyFolderName, yearFolderName });
            }
        }

        public async Task<IActionResult> SubCategoryFolder(string documentTypeFolderName, string departmentFolderName, string companyFolderName, string yearFolderName, CancellationToken cancellationToken)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;
            ViewBag.DocumentTypeFolder = documentTypeFolderName;

            var companyAccessResult = CheckCompanyAccess(companyFolderName);
            if (companyAccessResult != null)
            {
                return companyAccessResult;
            }

            var departmentAccessResult = CheckDepartmentAccess(departmentFolderName);
            if (departmentAccessResult != null)
            {
                return departmentAccessResult;
            }

            try
            {
                var subCategories = await _dmsQueryService.GetSubCategoriesAsync(companyFolderName, yearFolderName, departmentFolderName, documentTypeFolderName, cancellationToken);
                return View(subCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sub-categories for category {Category}.", documentTypeFolderName);
                TempData["error"] = "Failed to load category folder.";
                return RedirectToAction(nameof(DepartmentFolder), new { departmentFolderName, companyFolderName, yearFolderName });
            }
        }

        public async Task<IActionResult> DisplayFiles(string departmentFolderName,
            string companyFolderName,
            string yearFolderName,
            string documentTypeFolderName,
            string? subCategoryFolder,
            string? fileName,
            CancellationToken cancellation)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;
            ViewBag.DocumentTypeFolder = documentTypeFolderName;
            ViewBag.SubCategoryFolder = subCategoryFolder;
            ViewBag.CurrentFolder = subCategoryFolder ?? documentTypeFolderName;

            var companyAccessResult = CheckCompanyAccess(companyFolderName);
            if (companyAccessResult != null)
            {
                return companyAccessResult;
            }

            var departmentAccessResult = CheckDepartmentAccess(departmentFolderName);
            if (departmentAccessResult != null)
            {
                return departmentAccessResult;
            }

            try
            {
                var fileDocuments = await _dmsQueryService.GetFilesAsync(
                    companyFolderName,
                    yearFolderName,
                    departmentFolderName,
                    documentTypeFolderName,
                    subCategoryFolder,
                    fileName,
                    cancellation);

                return View(fileDocuments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to display files for department {Department}, category {Category}, sub-category {SubCategory}.", departmentFolderName, documentTypeFolderName, subCategoryFolder);
                TempData["error"] = "Failed to load files.";

                if (!string.IsNullOrWhiteSpace(subCategoryFolder))
                {
                    return RedirectToAction(nameof(SubCategoryFolder), new { documentTypeFolderName, departmentFolderName, companyFolderName, yearFolderName });
                }

                return RedirectToAction(nameof(DepartmentFolder), new { departmentFolderName, companyFolderName, yearFolderName });
            }
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetUploadedFiles([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dmsQueryService.GetUploadedFilesAsync(parameters, cancellationToken);

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

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            try
            {
                var files = await _userRepo.GetUploadedFiles(id, cancellationToken);
                if (files == null)
                {
                    return NotFound();
                }

                var documentAccessResult = EnsureDocumentMutationAccess(files);
                if (documentAccessResult != null)
                {
                    return documentAccessResult;
                }

                return View(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load document {DocumentId} for edit.", id);
                TempData["error"] = "Failed to load file document.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FileDocument model, IFormFile? newFile, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var file = await _dbContext.FileDocuments
                    .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

                if (file == null)
                {
                    return NotFound();
                }

                var documentAccessResult = EnsureDocumentMutationAccess(file);
                if (documentAccessResult != null)
                {
                    return documentAccessResult;
                }

                if (string.IsNullOrEmpty(model.BoxNumber))
                {
                    ModelState.AddModelError(nameof(FileDocument.BoxNumber), "");
                    TempData["error"] = "Box Number is required.";
                    return RedirectToAction("Edit", new { id = model.Id });
                }

                if (string.IsNullOrEmpty(model.SubmittedBy))
                {
                    TempData["error"] = "Submitted By is required.";
                    return RedirectToAction("Edit", new { id = model.Id });
                }

                if (model.DateSubmitted == null)
                {
                    TempData["error"] = "Date Submitted is required.";
                    return RedirectToAction("Edit", new { id = model.Id });
                }

                var detailsChanged = false;
                var fileChanged = false;
                var oldFileName = file.OriginalFilename;

                if (file.Description != model.Description ||
                    file.BoxNumber != model.BoxNumber ||
                    file.SubmittedBy != model.SubmittedBy ||
                    file.DateSubmitted != model.DateSubmitted)
                {
                    file.Description = model.Description;
                    file.BoxNumber = model.BoxNumber;
                    file.SubmittedBy = model.SubmittedBy;
                    file.DateSubmitted = model.DateSubmitted;
                    detailsChanged = true;
                }

                if (newFile?.Length > 0)
                {
                    var pdfValidationResult = await _pdfUploadValidationService.ValidateAsync(newFile, cancellationToken);
                    if (!pdfValidationResult.IsValid)
                    {
                        TempData["error"] = pdfValidationResult.ErrorMessage;
                        return RedirectToAction("Edit", new { id = model.Id });
                    }

                    if (newFile.FileName != file.OriginalFilename &&
                        await _userRepo.CheckIfFileExists(newFile.FileName, cancellationToken))
                    {
                        TempData["error"] = "This file already exists in our database!";
                        return RedirectToAction("Edit", new { id = model.Id });
                    }

                    var replaceResult = await _documentStorageWorkflowService.ReplaceFileAsync(file, newFile, cancellationToken);

                    file.Name = replaceResult.StoredFileName;
                    file.Location = replaceResult.ObjectName;
                    file.FileSize = replaceResult.FileSize;
                    file.OriginalFilename = replaceResult.OriginalFileName;
                    file.NumberOfPages = pdfValidationResult.PageCount;
                    file.ExtractedText = string.Empty;
                    file.OcrStatus = OcrStatuses.Pending;
                    file.OcrQueuedAt = DateTimeHelper.GetCurrentPhilippineTime();
                    file.OcrStartedAt = null;
                    file.OcrCompletedAt = null;
                    file.OcrAttemptCount = 0;
                    file.OcrError = string.Empty;
                    fileChanged = true;
                }

                if (!detailsChanged && !fileChanged)
                {
                    TempData["info"] = "No changes were made.";
                    return RedirectToAction("Edit", new { id = model.Id });
                }

                var changeDescription = "";

                switch (detailsChanged)
                {
                    case true when fileChanged:
                        changeDescription = $"Updated details and replaced file in Cloud Storage for document# {file.Id} from {oldFileName} to {file.OriginalFilename}";
                        break;

                    case true:
                        changeDescription = $"Updated details for document# {file.Id}";
                        break;

                    default:
                    {
                        if (fileChanged)
                        {
                            changeDescription = $"Replaced file in Cloud Storage for document# {file.Id} from {oldFileName} to {file.OriginalFilename}";
                        }
                        break;
                    }
                }

                LogsModel logs = new(_accessService.Username!, changeDescription);
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = fileChanged
                    ? "File document updated successfully. Search text may take a while to refresh while text extraction completes."
                    : "File document updated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update document {DocumentId}.", model.Id);
                TempData["error"] = "Failed to update file document.";
                return RedirectToAction("Edit", new { id = model.Id });
            }
        }

        public async Task<IActionResult> GeneralSearch(
            string search,
            bool searchDocumentText = false,
            int page = 1,
            int pageSize = 10,
            string sortBy = "DateUploaded",
            string sortOrder = "desc",
            CancellationToken cancellationToken = default)
        {
            if (!_accessService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(search))
            {
                return View(new GeneralSearchViewModel
                {
                    CurrentPage = 1,
                    HasNextPage = false,
                    PageSize = pageSize,
                    SearchTerm = string.Empty,
                    SearchDocumentText = searchDocumentText,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                });
            }

            try
            {
                var model = await _dmsSearchService.SearchAsync(search, searchDocumentText, page, pageSize, sortBy, sortOrder, cancellationToken);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed general search for term {Search}.", search);
                TempData["error"] = "Failed to perform search.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDelete(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var model = await _dbContext
                .FileDocuments
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(model);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Delete from Cloud Storage
                await _cloudStorage.DeleteFileAsync(model.Location, cancellationToken);

                _dbContext.Remove(model);

                LogsModel logs = new(_accessService.Username!, $"Permanently delete the file from Cloud Storage: {model.Name}.");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "File has been permanently deleted from Cloud Storage.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred during file permanently deletion from Cloud Storage.");
                TempData["error"] = "Failed to permanently delete file from Cloud Storage.";
            }

            return RedirectToAction(nameof(Trash));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var model = await _dbContext
                .FileDocuments
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(model);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                model.IsDeleted = true;

                LogsModel logs = new(_accessService.Username!, $"Delete the file from Cloud Storage: {model.Name}.");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "File has been deleted from Cloud Storage.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred during file deletion from Cloud Storage.");
                TempData["error"] = "Failed to delete file from Cloud Storage.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var model = await _dbContext
                .FileDocuments
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(model);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                model.IsDeleted = false;

                LogsModel logs = new(_accessService.Username!, $"Restore the file from Cloud Storage: {model.Name}.");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "File has been restored from Cloud Storage.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred during file restoration from Cloud Storage.");
                TempData["error"] = "Failed to restore file from Cloud Storage.";
            }

            return RedirectToAction(nameof(Trash));
        }

        [HttpGet]
        public async Task<IActionResult> Transfer(int id, CancellationToken cancellationToken)
        {
            try
            {
                var files = await _userRepo.GetUploadedFiles(id, cancellationToken);
                if (files == null)
                {
                    return NotFound();
                }

                var documentAccessResult = EnsureDocumentMutationAccess(files);
                if (documentAccessResult != null)
                {
                    return documentAccessResult;
                }

                return View(await GetModelSelectList(files, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load transfer form for document {DocumentId}.", id);
                TempData["error"] = "Failed to load transfer form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(FileDocument model, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext
                .FileDocuments
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(existingModel);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
            }

            model = await GetModelSelectList(model, cancellationToken);

            if (_documentStorageWorkflowService.IsSameTransferLocation(existingModel, model))
            {
                ModelState.AddModelError(string.Empty, "The selected destination matches the current storage path. Change the destination before transferring.");
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var transferResult = await _documentStorageWorkflowService.TransferAsync(existingModel, model, cancellationToken);

                // Update model
                existingModel.Company = model.Company;
                existingModel.Year = model.Year;
                existingModel.Department = model.Department;
                existingModel.Category = model.Category;
                existingModel.SubCategory = model.SubCategory;
                existingModel.Name = transferResult.StoredFileName;
                existingModel.Location = transferResult.NewLocation;

                LogsModel logs = new(_accessService.Username!, $"Transfer the file in Cloud Storage: {existingModel.OriginalFilename} from {transferResult.OldLocation} to {transferResult.NewLocation}.");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "File successfully transferred in Cloud Storage.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred during file transfer in Cloud Storage.");
                TempData["error"] = "Failed to transfer file in Cloud Storage.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Download(int documentId, string originalFilename, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _dbContext.FileDocuments
                    .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

                if (document == null)
                {
                    return NotFound();
                }

                var companyAccessResult = CheckCompanyAccess(document.Company);
                if (companyAccessResult != null)
                {
                    return companyAccessResult;
                }

                var departmentAccessResult = CheckDepartmentAccess(document.Department);
                if (departmentAccessResult != null)
                {
                    return departmentAccessResult;
                }

                try
                {
                    var logs = new LogsModel(_accessService.Username!, $"Downloaded file from Cloud Storage: {originalFilename} from path: {document.Location}");
                    await _dbContext.Logs.AddAsync(logs, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write download audit log for file: {FileName}", originalFilename);
                }

                // Get signed URL for direct download (better performance)
                var signedUrl = await _cloudStorage.GetSignedUrlAsync(document.Location, TimeSpan.FromMinutes(5), cancellationToken);
                return Redirect(signedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from Cloud Storage: {FileName}", originalFilename);
                return BadRequest("Error downloading file from Cloud Storage");
            }
        }

        // Alternative download method that streams through the server
        [HttpGet]
        public async Task<IActionResult> DownloadDirect(int documentId, string originalFilename, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _dbContext.FileDocuments
                    .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

                if (document == null)
                {
                    return NotFound();
                }

                var companyAccessResult = CheckCompanyAccess(document.Company);
                if (companyAccessResult != null)
                {
                    return companyAccessResult;
                }

                var departmentAccessResult = CheckDepartmentAccess(document.Department);
                if (departmentAccessResult != null)
                {
                    return departmentAccessResult;
                }

                try
                {
                    var logs = new LogsModel(_accessService.Username!, $"Downloaded file directly from Cloud Storage: {originalFilename}");
                    await _dbContext.Logs.AddAsync(logs, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write direct download audit log for file: {FileName}", originalFilename);
                }

                // Download file from Cloud Storage and stream to user
                var fileStream = await _cloudStorage.DownloadFileStreamAsync(document.Location, cancellationToken);
                return File(fileStream, "application/pdf", originalFilename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file directly from Cloud Storage: {FileName}", originalFilename);
                return BadRequest("Error downloading file from Cloud Storage");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSubCategories(string categoryName)
        {
            try
            {
                if (string.IsNullOrEmpty(categoryName))
                {
                    return Json(new List<SelectListItem>());
                }

                var subCategories = await _dbContext.SubCategories
                    .Include(x => x.Category)
                    .Where(x => x.Category.CategoryName == categoryName)
                    .Select(sc => new SelectListItem
                    {
                        Value = sc.SubCategoryName,
                        Text = sc.SubCategoryName
                    })
                    .ToListAsync();

                return Json(subCategories);
            }
            catch
            {
                return Json(new List<SelectListItem>());
            }
        }

        [HttpGet]
        public IActionResult Trash()
        {
            if (!_accessService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                if (_accessService.CanAccessTrash())
                {
                    return View();
                }

                TempData["ErrorMessage"] = "You have no access to trash. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load trash view.");
                TempData["ErrorMessage"] = "Failed to load trash.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetDeletedFiles([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _dmsQueryService.GetDeletedFilesAsync(parameters, cancellationToken);

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
