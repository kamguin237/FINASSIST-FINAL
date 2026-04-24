using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M017_AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilisateurId = table.Column<int>(type: "int", nullable: false),
                    NotifApp = table.Column<bool>(type: "bit", nullable: false),
                    NotifEmail = table.Column<bool>(type: "bit", nullable: false),
                    AlertNouveauBesoin = table.Column<bool>(type: "bit", nullable: false),
                    AlertValidation = table.Column<bool>(type: "bit", nullable: false),
                    AlertEnAttente = table.Column<bool>(type: "bit", nullable: false),
                    Langue = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FormatDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ItemsParPage = table.Column<int>(type: "int", nullable: false),
                    PageAccueil = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TriDefaut = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UtilisateurId",
                table: "UserPreferences",
                column: "UtilisateurId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferences");
        }
    }
}
