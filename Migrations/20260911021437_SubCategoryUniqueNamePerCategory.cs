using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document_Management.Migrations
{
    /// <inheritdoc />
    public partial class SubCategoryUniqueNamePerCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubCategories_CategoryId",
                table: "SubCategories");

            migrationBuilder.DropIndex(
                name: "IX_SubCategories_SubCategoryName",
                table: "SubCategories");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategories_CategoryId_SubCategoryName",
                table: "SubCategories",
                columns: new[] { "CategoryId", "SubCategoryName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Resolve subcategory names reused across different categories before reversing this migration.
            migrationBuilder.DropIndex(
                name: "IX_SubCategories_CategoryId_SubCategoryName",
                table: "SubCategories");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategories_CategoryId",
                table: "SubCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategories_SubCategoryName",
                table: "SubCategories",
                column: "SubCategoryName",
                unique: true);
        }
    }
}
