using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowNomUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Workflows_Nom",
                table: "Workflows",
                column: "Nom",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workflows_Nom",
                table: "Workflows");
        }
    }
}
