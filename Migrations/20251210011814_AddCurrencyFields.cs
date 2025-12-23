using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrateraPos.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "BusinessSettings",
                newName: "CurrencySymbol");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "BusinessSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "BusinessSettings");

            migrationBuilder.RenameColumn(
                name: "CurrencySymbol",
                table: "BusinessSettings",
                newName: "Currency");
        }
    }
}
