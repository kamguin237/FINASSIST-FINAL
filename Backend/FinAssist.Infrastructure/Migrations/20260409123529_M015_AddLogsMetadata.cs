using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M015_AddLogsMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdresseIp",
                table: "LogsActivites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Navigateur",
                table: "LogsActivites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pays",
                table: "LogsActivites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemeExploitation",
                table: "LogsActivites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ville",
                table: "LogsActivites",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdresseIp",
                table: "LogsActivites");

            migrationBuilder.DropColumn(
                name: "Navigateur",
                table: "LogsActivites");

            migrationBuilder.DropColumn(
                name: "Pays",
                table: "LogsActivites");

            migrationBuilder.DropColumn(
                name: "SystemeExploitation",
                table: "LogsActivites");

            migrationBuilder.DropColumn(
                name: "Ville",
                table: "LogsActivites");
        }
    }
}
