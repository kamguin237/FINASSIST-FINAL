using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M009_AddNotificationsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Lu = table.Column<bool>(type: "bit", nullable: false),
                    DateEnvoi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UTILISATEUR_NOTIFICATION",
                columns: table => new
                {
                    UtilisateurId = table.Column<int>(type: "int", nullable: false),
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    Lu = table.Column<bool>(type: "bit", nullable: false),
                    DateLecture = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UTILISATEUR_NOTIFICATION", x => new { x.UtilisateurId, x.NotificationId });
                    table.ForeignKey(
                        name: "FK_UTILISATEUR_NOTIFICATION_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UTILISATEUR_NOTIFICATION_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UTILISATEUR_NOTIFICATION_NotificationId",
                table: "UTILISATEUR_NOTIFICATION",
                column: "NotificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UTILISATEUR_NOTIFICATION");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
