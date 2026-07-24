using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WFAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteredUniqueIndexesForCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Categories_NormalizedName",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "UX_Categories_NormalizedSlug",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "UX_Categories_NormalizedName",
                table: "Categories",
                column: "NormalizedName",
                unique: true,
                filter: "[SoftDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Categories_NormalizedSlug",
                table: "Categories",
                column: "NormalizedSlug",
                unique: true,
                filter: "[SoftDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Categories_NormalizedName",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "UX_Categories_NormalizedSlug",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "UX_Categories_NormalizedName",
                table: "Categories",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Categories_NormalizedSlug",
                table: "Categories",
                column: "NormalizedSlug",
                unique: true);
        }
    }
}