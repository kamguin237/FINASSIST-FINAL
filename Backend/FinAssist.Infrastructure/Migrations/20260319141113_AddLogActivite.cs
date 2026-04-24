using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLogActivite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogsActivites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntiteType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntiteId = table.Column<int>(type: "int", nullable: true),
                    AncienneValeur = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NouvelleValeur = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UtilisateurId = table.Column<int>(type: "int", nullable: true),
                    AdresseIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsActivites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsActivites_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogsActivites_UtilisateurId",
                table: "LogsActivites",
                column: "UtilisateurId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogsActivites");
        }
    }
}
