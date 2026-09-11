using Document_Management.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Data
{
    public static class ApplicationDbSeeder
    {
        private const string _adminUsername = "azh";
        private const string _adminPassword = "azh";

        public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminExists = await dbContext.Accounts
                .AnyAsync(x => x.Username == _adminUsername, cancellationToken);

            if (adminExists)
            {
                return;
            }

            var adminAccount = new Account
            {
                EmployeeNumber = "9999",
                FirstName = "AZH",
                LastName = "ADMIN",
                Username = _adminUsername,
                Role = "admin",
                Department = "MIS",
                AccessDepartments = string.Empty,
                AccessCompanies = string.Empty,
                ModuleAccess = "DMS",
                IsActive = true
            };

            var passwordHasher = new PasswordHasher<Account>();
            adminAccount.Password = passwordHasher.HashPassword(adminAccount, _adminPassword);

            await dbContext.Accounts.AddAsync(adminAccount, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
