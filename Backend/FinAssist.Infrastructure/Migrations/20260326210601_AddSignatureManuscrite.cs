using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureManuscrite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Hauteur",
                table: "Signatures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Largeur",
                table: "Signatures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PositionX",
                table: "Signatures",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PositionY",
                table: "Signatures",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureBase64",
                table: "Signatures",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hauteur",
                table: "Signatures");

            migrationBuilder.DropColumn(
                name: "Largeur",
                table: "Signatures");

            migrationBuilder.DropColumn(
                name: "PositionX",
                table: "Signatures");

            migrationBuilder.DropColumn(
                name: "PositionY",
                table: "Signatures");

            migrationBuilder.DropColumn(
                name: "SignatureBase64",
                table: "Signatures");
        }
    }
}
