using System.Security.Cryptography;
using System.Text;
using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Document_Management.Service
{
    public sealed class AccountManagementService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AccountManagementService> _logger;
        private static readonly PasswordHasher<Account> PasswordHasher = new();

        public AccountManagementService(ApplicationDbContext dbContext, ILogger<AccountManagementService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<SelectListItem>> GetDepartmentOptionsAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Departments
                .OrderBy(department => department.DepartmentName)
                .Select(department => new SelectListItem
                {
                    Text = department.DepartmentName,
                    Value = department.DepartmentName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCompanyOptionsAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Companies
                .OrderBy(company => company.CompanyName)
                .Select(company => new SelectListItem
                {
                    Text = company.CompanyName,
                    Value = company.CompanyName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<OperationResult> CreateAsync(
            AccountCreateViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            var validation = await ValidateCommonAsync(
                model.EmployeeNumber,
                model.Username,
                model.Role,
                model.Department,
                model.AccessDepartments,
                model.AccessCompanies,
                null,
                cancellationToken);
            if (validation != null)
            {
                return validation;
            }

            var credentialValidation = ValidateCreateCredentials(model);
            if (credentialValidation != null)
            {
                return credentialValidation;
            }

            var account = new Account
            {
                EmployeeNumber = model.EmployeeNumber,
                FirstName = model.FirstName.ToUpperInvariant(),
                LastName = model.LastName.ToUpperInvariant(),
                Username = model.Username,
                Role = model.Role,
                Department = model.Department,
                AccessDepartments = JoinSelections(model.AccessDepartments),
                AccessCompanies = JoinSelections(model.AccessCompanies),
                Password = string.Empty,
                IsActive = model.IsActive
            };

            account.Password = HashPassword(account, model.Password);

            await _dbContext.Accounts.AddAsync(account, cancellationToken);
            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Add new user: {account.Username}"), cancellationToken);

            return await SaveAsync("Failed to create user.", cancellationToken);
        }

        public async Task<OperationResult> UpdateAsync(
            AccountEditViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
            if (account == null)
            {
                return OperationResult.NotFoundResult();
            }

            var validation = await ValidateCommonAsync(
                model.EmployeeNumber,
                null,
                model.Role,
                model.Department,
                model.AccessDepartments,
                model.AccessCompanies,
                model.Id,
                cancellationToken);
            if (validation != null)
            {
                return validation;
            }

            account.EmployeeNumber = model.EmployeeNumber;
            account.FirstName = model.FirstName.ToUpperInvariant();
            account.LastName = model.LastName.ToUpperInvariant();
            account.Role = model.Role;
            account.Department = model.Department;
            account.AccessDepartments = JoinSelections(model.AccessDepartments);
            account.AccessCompanies = JoinSelections(model.AccessCompanies);
            account.IsActive = model.IsActive;

            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Update user: {account.Username}"), cancellationToken);

            return await SaveAsync("Failed to update user.", cancellationToken);
        }

        public async Task<OperationResult> UpdatePasswordAsync(
            AccountPasswordViewModel model,
            string actor,
            CancellationToken cancellationToken)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
            if (account == null)
            {
                return OperationResult.NotFoundResult();
            }

            var validation = ValidatePasswordChange(model, account);
            if (validation != null)
            {
                return validation;
            }

            account.Password = HashPassword(account, model.Password);
            await _dbContext.Logs.AddAsync(new LogsModel(actor, $"Update password for user: {account.Username}"), cancellationToken);

            return await SaveAsync("Failed to update password.", cancellationToken);
        }

        private async Task<OperationResult?> ValidateCommonAsync(
            string employeeNumber,
            string? username,
            string role,
            string department,
            string[]? accessDepartments,
            string[]? accessCompanies,
            int? currentId,
            CancellationToken cancellationToken)
        {
            var duplicateUsername = username != null && await _dbContext.Accounts.AnyAsync(
                account => account.Username == username && (!currentId.HasValue || account.Id != currentId.Value),
                cancellationToken);
            var duplicateEmployeeNumber = await _dbContext.Accounts.AnyAsync(
                account => account.EmployeeNumber == employeeNumber && (!currentId.HasValue || account.Id != currentId.Value),
                cancellationToken);

            if (duplicateUsername && duplicateEmployeeNumber)
            {
                return OperationResult.Validation(new Dictionary<string, string>
                {
                    [nameof(AccountCreateViewModel.Username)] = "Username is already in use by another user.",
                    [nameof(AccountCreateViewModel.EmployeeNumber)] = "Employee Number is already in use by another user."
                });
            }

            if (duplicateUsername)
            {
                return OperationResult.Validation(nameof(AccountCreateViewModel.Username), "Username is already in use by another user.");
            }

            if (duplicateEmployeeNumber)
            {
                return OperationResult.Validation(nameof(AccountCreateViewModel.EmployeeNumber), "Employee Number is already in use by another user.");
            }

            if (!Enum.GetNames<Roles>().Any(allowedRole => string.Equals(allowedRole, role, StringComparison.OrdinalIgnoreCase)))
            {
                return OperationResult.Validation(nameof(AccountCreateViewModel.Role), "Select a valid role.");
            }

            if (string.IsNullOrWhiteSpace(department)
                || !await _dbContext.Departments.AnyAsync(item => item.DepartmentName == department, cancellationToken))
            {
                return OperationResult.Validation(nameof(AccountCreateViewModel.Department), "Select an existing department.");
            }

            var departments = accessDepartments ?? Array.Empty<string>();
            var companies = accessCompanies ?? Array.Empty<string>();
            var existingDepartments = await _dbContext.Departments
                .Where(item => departments.Contains(item.DepartmentName))
                .Select(item => item.DepartmentName)
                .ToListAsync(cancellationToken);
            var invalidDepartment = departments.FirstOrDefault(item => !existingDepartments.Contains(item, StringComparer.Ordinal));
            if (invalidDepartment != null)
            {
                return OperationResult.Validation(nameof(AccountCreateViewModel.AccessDepartments), "Access includes a department that no longer exists.");
            }

            var existingCompanies = await _dbContext.Companies
                .Where(item => companies.Contains(item.CompanyName))
                .Select(item => item.CompanyName)
                .ToListAsync(cancellationToken);
            var invalidCompany = companies.FirstOrDefault(item => !existingCompanies.Contains(item, StringComparer.Ordinal));
            return invalidCompany != null
                ? OperationResult.Validation(nameof(AccountCreateViewModel.AccessCompanies), "Access includes a company that no longer exists.")
                : null;
        }

        private static OperationResult? ValidateCreateCredentials(AccountCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                return OperationResult.Validation(nameof(model.Password), "Password is required.");
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                return OperationResult.Validation(nameof(model.ConfirmPassword), "Confirm Password is required.");
            }

            return model.Password == model.ConfirmPassword
                ? null
                : OperationResult.Validation(nameof(model.ConfirmPassword), "Passwords do not match.");
        }

        private static OperationResult? ValidatePasswordChange(AccountPasswordViewModel model, Account account)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                return OperationResult.Validation(nameof(model.Password), "Password is required.");
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                return OperationResult.Validation(nameof(model.ConfirmPassword), "Confirm Password is required.");
            }

            if (model.Password != model.ConfirmPassword)
            {
                return OperationResult.Validation(nameof(model.ConfirmPassword), "Passwords do not match.");
            }

            return PasswordMatches(account, model.Password)
                ? OperationResult.Validation(nameof(model.Password), "New password must not be the same as the previous password.")
                : null;
        }

        private async Task<OperationResult> SaveAsync(string failureMessage, CancellationToken cancellationToken)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return OperationResult.Success();
            }
            catch (DbUpdateException ex) when (TryGetPostgresException(ex, out var postgresException))
            {
                if (postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    if (postgresException.ConstraintName == "IX_Accounts_Username")
                    {
                        return OperationResult.Validation(nameof(AccountCreateViewModel.Username), "Username is already in use by another user.");
                    }

                    if (postgresException.ConstraintName == "IX_Accounts_EmployeeNumber")
                    {
                        return OperationResult.Validation(nameof(AccountCreateViewModel.EmployeeNumber), "Employee Number is already in use by another user.");
                    }
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

        private static string JoinSelections(string[]? selections)
        {
            return selections == null || selections.Length == 0
                ? string.Empty
                : string.Join(",", selections);
        }

        private static string HashPassword(Account account, string password)
        {
            return PasswordHasher.HashPassword(account, password);
        }

        private static bool PasswordMatches(Account account, string password)
        {
            var verificationResult = PasswordHasher.VerifyHashedPassword(account, account.Password, password);
            if (verificationResult != PasswordVerificationResult.Failed)
            {
                return true;
            }

            var legacyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            return legacyHash == account.Password;
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
