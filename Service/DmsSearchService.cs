using Document_Management.Data;
using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Service
{
    public interface IDmsSearchService
    {
        Task<GeneralSearchViewModel> SearchAsync(
            string search,
            int page,
            int pageSize,
            string sortBy,
            string sortOrder,
            CancellationToken cancellationToken);
    }

    public sealed class DmsSearchService : IDmsSearchService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IDmsAccessService _accessService;

        public DmsSearchService(ApplicationDbContext dbContext, IDmsAccessService accessService)
        {
            _dbContext = dbContext;
            _accessService = accessService;
        }

        public async Task<GeneralSearchViewModel> SearchAsync(
            string search,
            int page,
            int pageSize,
            string sortBy,
            string sortOrder,
            CancellationToken cancellationToken)
        {
            page = Math.Max(1, page);
            pageSize = pageSize switch
            {
                <= 10 => 10,
                <= 25 => 25,
                <= 50 => 50,
                _ => 100
            };

            var keywords = search
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var query = _dbContext.FileDocuments
                .AsNoTracking()
                .Where(file => !file.IsDeleted);

            if (!_accessService.IsAdmin())
            {
                var accessibleCompanies = _accessService.GetAccessibleCompanies()
                    .ToArray();
                var accessibleDepartments = _accessService.GetAccessibleDepartments()
                    .ToArray();

                if (accessibleCompanies.Length == 0 || accessibleDepartments.Length == 0)
                {
                    return new GeneralSearchViewModel
                    {
                        Results = [],
                        CurrentPage = page,
                        TotalPages = 0,
                        TotalRecords = 0,
                        PageSize = pageSize,
                        SearchTerm = search,
                        SortBy = sortBy,
                        SortOrder = sortOrder
                    };
                }

                query = query.Where(file =>
                    accessibleCompanies.Contains(file.Company) &&
                    accessibleDepartments.Contains(file.Department));
            }

            foreach (var keyword in keywords)
            {
                var currentKeyword = $"%{keyword}%";
                query = query.Where(file =>
                    EF.Functions.ILike(file.Description, currentKeyword) ||
                    EF.Functions.ILike(file.OriginalFilename, currentKeyword) ||
                    EF.Functions.ILike(file.BoxNumber, currentKeyword) ||
                    EF.Functions.ILike(file.ExtractedText, currentKeyword));
            }

            var totalRecords = await query.CountAsync(cancellationToken);
            var totalPages = totalRecords == 0
                ? 0
                : (int)Math.Ceiling(totalRecords / (double)pageSize);

            var pagedResults = await ApplySorting(query, sortBy, sortOrder)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(file => new FileDocument
                {
                    Id = file.Id,
                    Company = file.Company,
                    Year = file.Year,
                    Department = file.Department,
                    Category = file.Category,
                    BoxNumber = file.BoxNumber,
                    OriginalFilename = file.OriginalFilename,
                    Description = file.Description,
                    Username = file.Username,
                    DateUploaded = file.DateUploaded
                })
                .ToListAsync(cancellationToken);

            return new GeneralSearchViewModel
            {
                Results = pagedResults,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                PageSize = pageSize,
                SearchTerm = search,
                SortBy = sortBy,
                SortOrder = sortOrder
            };
        }

        private static IQueryable<FileDocument> ApplySorting(IQueryable<FileDocument> query, string sortBy, string sortOrder)
        {
            return sortBy switch
            {
                "BoxNumber" => sortOrder == "asc"
                    ? query.OrderBy(file => file.BoxNumber)
                    : query.OrderByDescending(file => file.BoxNumber),
                "OriginalFilename" => sortOrder == "asc"
                    ? query.OrderBy(file => file.OriginalFilename)
                    : query.OrderByDescending(file => file.OriginalFilename),
                "Description" => sortOrder == "asc"
                    ? query.OrderBy(file => file.Description)
                    : query.OrderByDescending(file => file.Description),
                "Username" => sortOrder == "asc"
                    ? query.OrderBy(file => file.Username)
                    : query.OrderByDescending(file => file.Username),
                "DateUploaded" => sortOrder == "asc"
                    ? query.OrderBy(file => file.DateUploaded)
                    : query.OrderByDescending(file => file.DateUploaded),
                _ => query.OrderByDescending(file => file.DateUploaded)
            };
        }
    }
}
