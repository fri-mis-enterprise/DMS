using Document_Management.Data;
using Document_Management.Dtos;
using Document_Management.Models;
using Document_Management.Utility.Helper;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Service
{
    public interface IDmsQueryService
    {
        Task<List<string>> GetAccessibleCompaniesAsync(CancellationToken cancellationToken);
        Task<List<string>> GetYearsAsync(string company, CancellationToken cancellationToken);
        Task<List<string>> GetAccessibleDepartmentsAsync(string company, string year, CancellationToken cancellationToken);
        Task<List<CategoryDto>> GetCategoriesAsync(string company, string year, string department, CancellationToken cancellationToken);
        Task<List<string>> GetSubCategoriesAsync(string company, string year, string department, string category, CancellationToken cancellationToken);
        Task<List<FileDocument>> GetFilesAsync(string company, string year, string department, string category, string? subCategory, string? fileName, CancellationToken cancellationToken);
        Task<HomeDashboardViewModel> GetDashboardStatisticsAsync(CancellationToken cancellationToken);
        Task<DataTableResult<UploadedFilesViewModel>> GetUploadedFilesAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
        Task<DataTableResult<UploadedFilesViewModel>> GetDeletedFilesAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
    }

    public sealed class DmsQueryService : IDmsQueryService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IDmsAccessService _accessService;

        public DmsQueryService(ApplicationDbContext dbContext, IDmsAccessService accessService)
        {
            _dbContext = dbContext;
            _accessService = accessService;
        }

        public async Task<List<string>> GetAccessibleCompaniesAsync(CancellationToken cancellationToken)
        {
            var companies = await _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => !string.IsNullOrEmpty(f.Company))
                .Select(f => f.Company)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);

            return companies
                .Where(company => _accessService.CanAccessCompany(company))
                .ToList();
        }

        public Task<List<string>> GetYearsAsync(string company, CancellationToken cancellationToken)
        {
            return _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => f.Company == company && !string.IsNullOrEmpty(f.Year))
                .Select(f => f.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<string>> GetAccessibleDepartmentsAsync(string company, string year, CancellationToken cancellationToken)
        {
            var departments = await _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => f.Company == company &&
                            f.Year == year &&
                            !string.IsNullOrEmpty(f.Department))
                .Select(f => f.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync(cancellationToken);

            return departments
                .Where(department => _accessService.CanAccessDepartment(department))
                .ToList();
        }

        public Task<List<CategoryDto>> GetCategoriesAsync(string company, string year, string department, CancellationToken cancellationToken)
        {
            return _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => f.Company == company &&
                            f.Year == year &&
                            f.Department == department &&
                            !string.IsNullOrEmpty(f.Category))
                .Select(f => new CategoryDto
                {
                    Category = f.Category,
                    SubCategory = f.SubCategory
                })
                .Distinct()
                .OrderBy(c => c.Category)
                .ToListAsync(cancellationToken);
        }

        public Task<List<string>> GetSubCategoriesAsync(string company, string year, string department, string category, CancellationToken cancellationToken)
        {
            return _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => f.Company == company &&
                            f.Year == year &&
                            f.Department == department &&
                            f.Category == category &&
                            !string.IsNullOrEmpty(f.SubCategory) &&
                            f.SubCategory != "N/A")
                .Select(f => f.SubCategory)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync(cancellationToken);
        }

        public Task<List<FileDocument>> GetFilesAsync(string company, string year, string department, string category, string? subCategory, string? fileName, CancellationToken cancellationToken)
        {
            var query = _dbContext.FileDocuments
                .AsNoTracking()
                .Where(file => file.Company == company
                               && file.Year == year
                               && file.Department == department
                               && file.Category == category
                               && !file.IsDeleted);

            query = !string.IsNullOrEmpty(subCategory)
                ? query.Where(file => file.SubCategory == subCategory)
                : query.Where(file => file.SubCategory == "N/A" || string.IsNullOrEmpty(file.SubCategory));

            if (!string.IsNullOrEmpty(fileName))
            {
                query = query.Where(file => file.Name == fileName);
            }

            return query
                .Select(file => new FileDocument
                {
                    Id = file.Id,
                    Name = file.Name,
                    Location = file.Location,
                    DateUploaded = file.DateUploaded,
                    Description = file.Description,
                    Department = file.Department,
                    Username = file.Username,
                    Category = file.Category,
                    Company = file.Company,
                    Year = file.Year,
                    SubCategory = file.SubCategory,
                    OriginalFilename = file.OriginalFilename,
                    FileSize = file.FileSize,
                    NumberOfPages = file.NumberOfPages
                })
                .OrderByDescending(file => file.DateUploaded)
                .ToListAsync(cancellationToken);
        }

        public async Task<HomeDashboardViewModel> GetDashboardStatisticsAsync(CancellationToken cancellationToken)
        {
            var query = _dbContext.FileDocuments
                .AsNoTracking()
                .Where(file => !file.IsDeleted);

            if (!_accessService.IsAdmin())
            {
                var accessibleCompanies = _accessService.GetAccessibleCompanies();
                var accessibleDepartments = _accessService.GetAccessibleDepartments();

                if (accessibleCompanies.Count == 0 || accessibleDepartments.Count == 0)
                {
                    return new HomeDashboardViewModel();
                }

                query = query.Where(file =>
                    accessibleCompanies.Contains(file.Company) &&
                    accessibleDepartments.Contains(file.Department));
            }

            var currentDate = DateTimeHelper.GetCurrentPhilippineTime();
            var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            return await query
                .GroupBy(_ => 1)
                .Select(files => new HomeDashboardViewModel
                {
                    ActiveDocuments = files.LongCount(),
                    UploadedThisMonth = files.LongCount(file =>
                        file.DateUploaded >= monthStart && file.DateUploaded < nextMonthStart),
                    TotalPages = files.Sum(file => (long)file.NumberOfPages),
                    StorageUsedBytes = files.Sum(file => file.FileSize)
                })
                .SingleOrDefaultAsync(cancellationToken)
                ?? new HomeDashboardViewModel();
        }

        public Task<DataTableResult<UploadedFilesViewModel>> GetUploadedFilesAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return GetFileTableAsync(parameters, includeDeleted: false, cancellationToken);
        }

        public Task<DataTableResult<UploadedFilesViewModel>> GetDeletedFilesAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return GetFileTableAsync(parameters, includeDeleted: true, cancellationToken);
        }

        private async Task<DataTableResult<UploadedFilesViewModel>> GetFileTableAsync(DataTablesParameters parameters, bool includeDeleted, CancellationToken cancellationToken)
        {
            var username = _accessService.Username;
            var filesQuery = _dbContext.FileDocuments
                .AsNoTracking()
                .Where(file => file.IsDeleted == includeDeleted);

            if (!_accessService.IsAdmin())
            {
                filesQuery = filesQuery.Where(file => file.Username == username);
            }

            var totalRecordsBeforeSearch = await filesQuery.CountAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(parameters.Search.Value))
            {
                var searchValue = parameters.Search.Value.Trim();
                var hasParsedDate = DateTime.TryParse(searchValue, out var searchDate);
                var searchDateStart = searchDate.Date;
                var searchDateEnd = searchDateStart.AddDays(1);

                filesQuery = filesQuery.Where(file =>
                    EF.Functions.ILike(file.Name, $"%{searchValue}%") ||
                    EF.Functions.ILike(file.Description, $"%{searchValue}%") ||
                    EF.Functions.ILike(file.Username, $"%{searchValue}%") ||
                    EF.Functions.ILike(file.BoxNumber, $"%{searchValue}%") ||
                    EF.Functions.ILike(file.SubmittedBy, $"%{searchValue}%") ||
                    EF.Functions.ILike(file.Company, $"%{searchValue}%") ||
                    EF.Functions.ILike(file.Year, $"%{searchValue}%") ||
                    EF.Functions.ILike(file.Department, $"%{searchValue}%") ||
                    EF.Functions.ILike(file.Category, $"%{searchValue}%") ||
                    EF.Functions.ILike(file.SubCategory, $"%{searchValue}%") ||
                    (hasParsedDate &&
                     file.DateUploaded >= searchDateStart &&
                     file.DateUploaded < searchDateEnd));
            }

            var recordsFiltered = await filesQuery.CountAsync(cancellationToken);

            if (parameters.Order.Count > 0)
            {
                var orderColumn = parameters.Order[0];

                if (orderColumn.Column >= 0 && orderColumn.Column < parameters.Columns.Count)
                {
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var isAscending = orderColumn.Dir.Equals("asc", StringComparison.CurrentCultureIgnoreCase);

                    filesQuery = ApplyUploadedFilesOrdering(filesQuery, columnName, isAscending);
                }
            }
            else
            {
                filesQuery = filesQuery.OrderByDescending(file => file.DateUploaded);
            }

            var pagedData = await filesQuery
                .Skip(parameters.Start)
                .Take(parameters.Length)
                .Select(file => new UploadedFilesViewModel
                {
                    Id = file.Id,
                    Name = file.Name,
                    Description = file.Description,
                    LocationFolder = file.SubCategory == "N/A"
                        ? $"companyFolderName={file.Company}&yearFolderName={file.Year}&departmentFolderName={file.Department}&documentTypeFolderName={file.Category}&subCategoryFolder={null}&fileName={file.Name}"
                        : $"companyFolderName={file.Company}&yearFolderName={file.Year}&departmentFolderName={file.Department}&documentTypeFolderName={file.Category}&subCategoryFolder={file.SubCategory}&fileName={file.Name}",
                    UploadedBy = file.Username,
                    DateUploaded = file.DateUploaded,
                    BoxNumber = file.BoxNumber,
                    SubmittedBy = file.SubmittedBy,
                    DateSubmitted = file.DateSubmitted ?? default
                })
                .ToListAsync(cancellationToken);

            return new DataTableResult<UploadedFilesViewModel>(parameters.Draw, totalRecordsBeforeSearch, recordsFiltered, pagedData);
        }

        private static IQueryable<FileDocument> ApplyUploadedFilesOrdering(IQueryable<FileDocument> query, string columnName, bool isAscending)
        {
            return columnName switch
            {
                "name" => isAscending
                    ? query.OrderBy(file => file.Name)
                    : query.OrderByDescending(file => file.Name),
                "description" => isAscending
                    ? query.OrderBy(file => file.Description)
                    : query.OrderByDescending(file => file.Description),
                "uploadedBy" => isAscending
                    ? query.OrderBy(file => file.Username)
                    : query.OrderByDescending(file => file.Username),
                "dateUploaded" => isAscending
                    ? query.OrderBy(file => file.DateUploaded)
                    : query.OrderByDescending(file => file.DateUploaded),
                "boxNumber" => isAscending
                    ? query.OrderBy(file => file.BoxNumber)
                    : query.OrderByDescending(file => file.BoxNumber),
                "submittedBy" => isAscending
                    ? query.OrderBy(file => file.SubmittedBy)
                    : query.OrderByDescending(file => file.SubmittedBy),
                "dateSubmitted" => isAscending
                    ? query.OrderBy(file => file.DateSubmitted)
                    : query.OrderByDescending(file => file.DateSubmitted),
                _ => isAscending
                    ? query.OrderBy(file => file.DateUploaded)
                    : query.OrderByDescending(file => file.DateUploaded)
            };
        }
    }

    public sealed record DataTableResult<T>(int Draw, int RecordsTotal, int RecordsFiltered, IReadOnlyCollection<T> Data);
}
