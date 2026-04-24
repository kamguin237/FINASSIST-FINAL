using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicWorkflowCircuit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EtapeOrdre",
                table: "Validations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatutApres",
                table: "Validations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkflowId",
                table: "Categories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "WorkflowCircuitId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EtapeCouranteOrdre",
                table: "Besoins",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowCircuits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NomCreateur = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowCircuits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowCircuits_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EtapesCircuit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowCircuitId = table.Column<int>(type: "int", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    RoleRequis = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprobationRequise = table.Column<bool>(type: "bit", nullable: false),
                    SignatureRequise = table.Column<bool>(type: "bit", nullable: false),
                    DelaiMaxJours = table.Column<int>(type: "int", nullable: false),
                    EstDerniereEtape = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapesCircuit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapesCircuit_WorkflowCircuits_WorkflowCircuitId",
                        column: x => x.WorkflowCircuitId,
                        principalTable: "WorkflowCircuits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_WorkflowCircuitId",
                table: "Categories",
                column: "WorkflowCircuitId");

            migrationBuilder.CreateIndex(
                name: "IX_EtapesCircuit_WorkflowCircuitId",
                table: "EtapesCircuit",
                column: "WorkflowCircuitId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCircuits_WorkflowId",
                table: "WorkflowCircuits",
                column: "WorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_WorkflowCircuits_WorkflowCircuitId",
                table: "Categories",
                column: "WorkflowCircuitId",
                principalTable: "WorkflowCircuits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_WorkflowCircuits_WorkflowCircuitId",
                table: "Categories");

            migrationBuilder.DropTable(
                name: "EtapesCircuit");

            migrationBuilder.DropTable(
                name: "WorkflowCircuits");

            migrationBuilder.DropIndex(
                name: "IX_Categories_WorkflowCircuitId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "EtapeOrdre",
                table: "Validations");

            migrationBuilder.DropColumn(
                name: "StatutApres",
                table: "Validations");

            migrationBuilder.DropColumn(
                name: "WorkflowCircuitId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "EtapeCouranteOrdre",
                table: "Besoins");

            migrationBuilder.AlterColumn<int>(
                name: "WorkflowId",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
