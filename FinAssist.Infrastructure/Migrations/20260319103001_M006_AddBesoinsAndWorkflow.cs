using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M006_AddBesoinsAndWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Validations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Niveau = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Commentaire = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateDecision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidateurId = table.Column<int>(type: "int", nullable: false),
                    BesoinId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Validations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Validations_Besoins_BesoinId",
                        column: x => x.BesoinId,
                        principalTable: "Besoins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Validations_Utilisateurs_ValidateurId",
                        column: x => x.ValidateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NiveauValidation = table.Column<int>(type: "int", nullable: false),
                    ApprobationRequise = table.Column<bool>(type: "bit", nullable: false),
                    DelaiMaxJours = table.Column<int>(type: "int", nullable: false),
                    NomCreateurWorkflow = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_WorkflowId",
                table: "Categories",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Validations_BesoinId",
                table: "Validations",
                column: "BesoinId");

            migrationBuilder.CreateIndex(
                name: "IX_Validations_ValidateurId",
                table: "Validations",
                column: "ValidateurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Workflows_WorkflowId",
                table: "Categories",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Workflows_WorkflowId",
                table: "Categories");

            migrationBuilder.DropTable(
                name: "Validations");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Categories_WorkflowId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "Categories");
        }
    }
}
