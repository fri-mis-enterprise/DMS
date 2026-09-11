using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document_Management.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeNumberAsString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Accounts\" ALTER COLUMN \"EmployeeNumber\" TYPE varchar(4) USING \"EmployeeNumber\"::text;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Accounts\" ALTER COLUMN \"EmployeeNumber\" TYPE integer USING \"EmployeeNumber\"::integer;");
        }
    }
}
