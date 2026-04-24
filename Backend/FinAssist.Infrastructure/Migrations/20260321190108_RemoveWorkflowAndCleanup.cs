using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkflowAndCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Workflows_WorkflowId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowCircuits_Workflows_WorkflowId",
                table: "WorkflowCircuits");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowCircuits_WorkflowId",
                table: "WorkflowCircuits");

            migrationBuilder.DropIndex(
                name: "IX_Categories_WorkflowId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "WorkflowCircuits");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "Categories");

            migrationBuilder.AlterColumn<int>(
                name: "WorkflowCircuitId",
                table: "EtapesCircuit",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCircuits_Nom",
                table: "WorkflowCircuits",
                column: "Nom",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowCircuits_Nom",
                table: "WorkflowCircuits");

            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "WorkflowCircuits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "WorkflowCircuitId",
                table: "EtapesCircuit",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprobationRequise = table.Column<bool>(type: "bit", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DelaiMaxJours = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EtapesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NiveauValidation = table.Column<int>(type: "int", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NomCreateurWorkflow = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCircuits_WorkflowId",
                table: "WorkflowCircuits",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_WorkflowId",
                table: "Categories",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_Nom",
                table: "Workflows",
                column: "Nom",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Workflows_WorkflowId",
                table: "Categories",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowCircuits_Workflows_WorkflowId",
                table: "WorkflowCircuits",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
