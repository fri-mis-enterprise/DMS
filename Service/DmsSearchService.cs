using Document_Management.Data;
using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Service
{
    public interface IDmsSearchService
    {
        Task<GeneralSearchViewModel> SearchAsync(
            string search,
            bool searchDocumentText,
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
            bool searchDocumentText,
            int page,
            int pageSize,
            string sortBy,
            string sortOrder,
            CancellationToken cancellationToken)
        {
            search = search.Trim();
            page = Math.Max(1, page);
            pageSize = pageSize switch
            {
                <= 10 => 10,
                <= 25 => 25,
                <= 50 => 50,
                _ => 100
            };

            var searchExtractedTextOnly = searchDocumentText;
            if (search.StartsWith("ocr:", StringComparison.OrdinalIgnoreCase))
            {
                searchExtractedTextOnly = true;
                search = search[4..].Trim();
            }
            else if (search.StartsWith("content:", StringComparison.OrdinalIgnoreCase))
            {
                searchExtractedTextOnly = true;
                search = search[8..].Trim();
            }

            var normalizedSearch = search;

            if (string.IsNullOrWhiteSpace(normalizedSearch))
            {
                return new GeneralSearchViewModel
                {
                    Results = [],
                    CurrentPage = page,
                    HasNextPage = false,
                    PageSize = pageSize,
                    SearchTerm = normalizedSearch,
                    SearchDocumentText = searchExtractedTextOnly,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };
            }

            var keywords = normalizedSearch
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
                        HasNextPage = false,
                        PageSize = pageSize,
                        SearchTerm = normalizedSearch,
                        SearchDocumentText = searchExtractedTextOnly,
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
                var isNumericKeyword = keyword.All(char.IsDigit);
                var isYearKeyword = isNumericKeyword && keyword.Length == 4;

                if (searchExtractedTextOnly)
                {
                    query = query.Where(file => EF.Functions.ILike(file.ExtractedText, currentKeyword));
                    continue;
                }

                query = query.Where(file =>
                    EF.Functions.ILike(file.Description, currentKeyword) ||
                    EF.Functions.ILike(file.OriginalFilename, currentKeyword) ||
                    EF.Functions.ILike(file.BoxNumber, currentKeyword) ||
                    EF.Functions.ILike(file.Company, currentKeyword) ||
                    EF.Functions.ILike(file.Department, currentKeyword) ||
                    EF.Functions.ILike(file.Category, currentKeyword) ||
                    EF.Functions.ILike(file.SubCategory, currentKeyword) ||
                    EF.Functions.ILike(file.Username, currentKeyword) ||
                    EF.Functions.ILike(file.SubmittedBy, currentKeyword) ||
                    (isYearKeyword
                        ? file.Year == keyword
                        : EF.Functions.ILike(file.Year, currentKeyword)));
            }

            var pagedResults = await ApplySorting(query, sortBy, sortOrder)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
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

            var hasNextPage = pagedResults.Count > pageSize;
            if (hasNextPage)
            {
                pagedResults.RemoveAt(pagedResults.Count - 1);
            }

            return new GeneralSearchViewModel
            {
                Results = pagedResults,
                CurrentPage = page,
                HasNextPage = hasNextPage,
                PageSize = pageSize,
                SearchTerm = normalizedSearch,
                SearchDocumentText = searchExtractedTextOnly,
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
