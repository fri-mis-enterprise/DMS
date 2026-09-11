using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Utility.Extensions;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Document_Management.Service
{
    public sealed class MasterDataService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<MasterDataService> _logger;

        public MasterDataService(ApplicationDbContext dbContext, ILogger<MasterDataService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public static string NormalizeCompanyOrDepartmentName(string? name)
        {
            return name.RemoveCommas();
        }

        public async Task<List<SelectListItem>> GetCategoryOptionsAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Categories
                .OrderBy(category => category.CategoryName)
                .Select(category => new SelectListItem
                {
                    Text = category.CategoryName,
                    Value = category.Id.ToString()
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<OperationResult> CreateCompanyAsync(
            CompanyViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            model.CompanyName = NormalizeCompanyOrDepartmentName(model.CompanyName);
            var validation = ValidateName(model.CompanyName, nameof(model.CompanyName), "Company");
            if (validation != null)
            {
                return validation;
            }

            if (await _dbContext.Companies.AnyAsync(company => company.CompanyName == model.CompanyName, cancellationToken))
            {
                return OperationResult.Validation(nameof(model.CompanyName), "The company with the same name already exists.");
            }

            var company = new Company
            {
                CompanyName = model.CompanyName,
                CreatedBy = actor
            };

            await _dbContext.Companies.AddAsync(company, cancellationToken);
            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Add new company: {model.CompanyName}"), cancellationToken);

            return await SaveAsync(
                "Failed to create company.",
                "CompanyName",
                "IX_Companies_CompanyName",
                cancellationToken);
        }

        public async Task<OperationResult> UpdateCompanyAsync(
            CompanyViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            model.CompanyName = NormalizeCompanyOrDepartmentName(model.CompanyName);
            var validation = ValidateName(model.CompanyName, nameof(model.CompanyName), "Company");
            if (validation != null)
            {
                return validation;
            }

            var company = await _dbContext.Companies.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
            if (company == null)
            {
                return OperationResult.NotFoundResult();
            }

            if (await _dbContext.Companies.AnyAsync(
                    item => item.Id != model.Id && item.CompanyName == model.CompanyName,
                    cancellationToken))
            {
                return OperationResult.Validation(nameof(model.CompanyName), "The company with the same name already exists.");
            }

            var existingName = company.CompanyName;
            company.CompanyName = model.CompanyName;
            company.EditedBy = actor;
            company.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Update company from {existingName} to {model.CompanyName}"), cancellationToken);

            return await SaveAsync(
                "Failed to update company.",
                "CompanyName",
                "IX_Companies_CompanyName",
                cancellationToken);
        }

        public async Task<OperationResult> CreateDepartmentAsync(
            DepartmentViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            model.DepartmentName = NormalizeCompanyOrDepartmentName(model.DepartmentName);
            var validation = ValidateName(model.DepartmentName, nameof(model.DepartmentName), "Department");
            if (validation != null)
            {
                return validation;
            }

            if (await _dbContext.Departments.AnyAsync(department => department.DepartmentName == model.DepartmentName, cancellationToken))
            {
                return OperationResult.Validation(nameof(model.DepartmentName), "The department with the same name already exists.");
            }

            var department = new Department
            {
                DepartmentName = model.DepartmentName,
                CreatedBy = actor
            };

            await _dbContext.Departments.AddAsync(department, cancellationToken);
            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Add new department: {model.DepartmentName}"), cancellationToken);

            return await SaveAsync(
                "Failed to create department.",
                "DepartmentName",
                "IX_Departments_DepartmentName",
                cancellationToken);
        }

        public async Task<OperationResult> UpdateDepartmentAsync(
            DepartmentViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            model.DepartmentName = NormalizeCompanyOrDepartmentName(model.DepartmentName);
            var validation = ValidateName(model.DepartmentName, nameof(model.DepartmentName), "Department");
            if (validation != null)
            {
                return validation;
            }

            var department = await _dbContext.Departments.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
            if (department == null)
            {
                return OperationResult.NotFoundResult();
            }

            if (await _dbContext.Departments.AnyAsync(
                    item => item.Id != model.Id && item.DepartmentName == model.DepartmentName,
                    cancellationToken))
            {
                return OperationResult.Validation(nameof(model.DepartmentName), "The department with the same name already exists.");
            }

            var existingName = department.DepartmentName;
            department.DepartmentName = model.DepartmentName;
            department.EditedBy = actor;
            department.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Update department from {existingName} to {model.DepartmentName}"), cancellationToken);

            return await SaveAsync(
                "Failed to update department.",
                "DepartmentName",
                "IX_Departments_DepartmentName",
                cancellationToken);
        }

        public async Task<OperationResult> CreateCategoryAsync(
            CategoryViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            var validation = ValidateName(model.CategoryName, nameof(model.CategoryName), "Category");
            if (validation != null)
            {
                return validation;
            }

            if (await _dbContext.Categories.AnyAsync(category => category.CategoryName == model.CategoryName, cancellationToken))
            {
                return OperationResult.Validation(nameof(model.CategoryName), "The category with the same name already exists.");
            }

            var category = new Category
            {
                CategoryName = model.CategoryName,
                CreatedBy = actor
            };

            await _dbContext.Categories.AddAsync(category, cancellationToken);
            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Add new category: {model.CategoryName}"), cancellationToken);

            return await SaveAsync(
                "Failed to create category.",
                "CategoryName",
                "IX_Categories_CategoryName",
                cancellationToken);
        }

        public async Task<OperationResult> UpdateCategoryAsync(
            CategoryViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            var validation = ValidateName(model.CategoryName, nameof(model.CategoryName), "Category");
            if (validation != null)
            {
                return validation;
            }

            var category = await _dbContext.Categories.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
            if (category == null)
            {
                return OperationResult.NotFoundResult();
            }

            if (await _dbContext.Categories.AnyAsync(
                    item => item.Id != model.Id && item.CategoryName == model.CategoryName,
                    cancellationToken))
            {
                return OperationResult.Validation(nameof(model.CategoryName), "The category with the same name already exists.");
            }

            var existingName = category.CategoryName;
            category.CategoryName = model.CategoryName;
            category.EditedBy = actor;
            category.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Update category from {existingName} to {model.CategoryName}"), cancellationToken);

            return await SaveAsync(
                "Failed to update category.",
                "CategoryName",
                "IX_Categories_CategoryName",
                cancellationToken);
        }

        public async Task<OperationResult> CreateSubCategoryAsync(
            SubCategoryViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            var validation = await ValidateSubCategoryAsync(model, null, cancellationToken);
            if (validation != null)
            {
                return validation;
            }

            var subCategory = new SubCategory
            {
                SubCategoryName = model.SubCategoryName,
                CategoryId = model.CategoryId,
                CreatedBy = actor
            };

            await _dbContext.SubCategories.AddAsync(subCategory, cancellationToken);
            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Add new sub-category: {model.SubCategoryName}"), cancellationToken);

            return await SaveAsync(
                "Failed to create sub-category.",
                "SubCategoryName",
                "IX_SubCategories_CategoryId_SubCategoryName",
                cancellationToken);
        }

        public async Task<OperationResult> UpdateSubCategoryAsync(
            SubCategoryViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            var validation = await ValidateSubCategoryAsync(model, model.Id, cancellationToken);
            if (validation != null)
            {
                return validation;
            }

            var subCategory = await _dbContext.SubCategories.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
            if (subCategory == null)
            {
                return OperationResult.NotFoundResult();
            }

            var existingName = subCategory.SubCategoryName;
            subCategory.SubCategoryName = model.SubCategoryName;
            subCategory.CategoryId = model.CategoryId;
            subCategory.EditedBy = actor;
            subCategory.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Update sub-category from {existingName} to {model.SubCategoryName}"), cancellationToken);

            return await SaveAsync(
                "Failed to update sub-category.",
                "SubCategoryName",
                "IX_SubCategories_CategoryId_SubCategoryName",
                cancellationToken);
        }

        private async Task<OperationResult?> ValidateSubCategoryAsync(
            SubCategoryViewModel model,
            int? currentId,
            CancellationToken cancellationToken)
        {
            var validation = ValidateName(model.SubCategoryName, nameof(model.SubCategoryName), "Sub-category");
            if (validation != null)
            {
                return validation;
            }

            if (model.CategoryId <= 0 || !await _dbContext.Categories.AnyAsync(x => x.Id == model.CategoryId, cancellationToken))
            {
                return OperationResult.Validation(nameof(model.CategoryId), "Select an existing category.");
            }

            var duplicate = await _dbContext.SubCategories.AnyAsync(
                item => item.CategoryId == model.CategoryId
                        && item.SubCategoryName == model.SubCategoryName
                        && (!currentId.HasValue || item.Id != currentId.Value),
                cancellationToken);

            return duplicate
                ? OperationResult.Validation(nameof(model.SubCategoryName), "The sub-category with the same name already exists in this category.")
                : null;
        }

        private static OperationResult? ValidateName(string? name, string field, string displayName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return OperationResult.Validation(field, $"{displayName} name is required.");
            }

            if (name.Length > 100)
            {
                return OperationResult.Validation(field, $"{displayName} name cannot exceed 100 characters.");
            }

            return null;
        }

        private async Task<OperationResult> SaveAsync(
            string failureMessage,
            string field,
            string uniqueConstraint,
            CancellationToken cancellationToken)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return OperationResult.Success();
            }
            catch (DbUpdateException ex) when (TryGetPostgresException(ex, out var postgresException))
            {
                if (postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                    && postgresException.ConstraintName == uniqueConstraint)
                {
                    return OperationResult.Validation(field, $"The {field.Replace("Name", "", StringComparison.Ordinal)} with the same name already exists.");
                }

                if (postgresException.SqlState == PostgresErrorCodes.ForeignKeyViolation
                    && postgresException.ConstraintName == "FK_SubCategories_Categories_CategoryId")
                {
                    return OperationResult.Validation("CategoryId", "Select an existing category.");
                }

                _logger.LogError(ex, "{FailureMessage}", failureMessage);
                return OperationResult.Failure(failureMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{FailureMessage}", failureMessage);
                return OperationResult.Failure(failureMessage);
            }
        }

        private static bool TryGetPostgresException(DbUpdateException exception, out PostgresException postgresException)
        {
            postgresException = exception.InnerException as PostgresException
                ?? exception.InnerException?.InnerException as PostgresException
                ?? null!;
            return postgresException != null;
        }
    }
}
