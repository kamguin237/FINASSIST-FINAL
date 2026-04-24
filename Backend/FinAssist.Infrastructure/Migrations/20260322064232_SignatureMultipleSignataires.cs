using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SignatureMultipleSignataires : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Signatures_DocumentId",
                table: "Signatures");

            migrationBuilder.CreateIndex(
                name: "IX_Signatures_DocumentId_UtilisateurId",
                table: "Signatures",
                columns: new[] { "DocumentId", "UtilisateurId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Signatures_DocumentId_UtilisateurId",
                table: "Signatures");

            migrationBuilder.CreateIndex(
                name: "IX_Signatures_DocumentId",
                table: "Signatures",
                column: "DocumentId",
                unique: true);
        }
    }
}
