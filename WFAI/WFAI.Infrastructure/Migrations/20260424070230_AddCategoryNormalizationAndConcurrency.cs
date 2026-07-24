using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WFAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryNormalizationAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Categories",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedSlug",
                table: "Categories",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Categories]
                SET
                    [NormalizedName] = UPPER(LTRIM(RTRIM([Name]))),
                    [NormalizedSlug] = UPPER(LTRIM(RTRIM([Slug])))
                """);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Categories",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedSlug",
                table: "Categories",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Categories_NormalizedName",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "UX_Categories_NormalizedSlug",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "NormalizedSlug",
                table: "Categories");
        }
    }
}