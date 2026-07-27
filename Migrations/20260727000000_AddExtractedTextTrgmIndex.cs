using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document_Management.Migrations
{
    [DbContext(typeof(Data.ApplicationDbContext))]
    [Migration("20260727000000_AddExtractedTextTrgmIndex")]
    public partial class AddExtractedTextTrgmIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pg_trgm;
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_FileDocuments_ExtractedText_Trgm"
                ON "FileDocuments" USING gin ("ExtractedText" gin_trgm_ops)
                WHERE NOT "IsDeleted";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_FileDocuments_ExtractedText_Trgm";""");
        }
    }
}
