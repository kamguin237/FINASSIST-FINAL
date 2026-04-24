using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M021_AddDeadlineTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ValidateurId",
                table: "Validations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateEntreeEnAttente",
                table: "Besoins",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailRappelEnvoye",
                table: "Besoins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Rappel1Envoye",
                table: "Besoins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Rappel2Envoye",
                table: "Besoins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RejeteAutomatiquement",
                table: "Besoins",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateEntreeEnAttente",
                table: "Besoins");

            migrationBuilder.DropColumn(
                name: "EmailRappelEnvoye",
                table: "Besoins");

            migrationBuilder.DropColumn(
                name: "Rappel1Envoye",
                table: "Besoins");

            migrationBuilder.DropColumn(
                name: "Rappel2Envoye",
                table: "Besoins");

            migrationBuilder.DropColumn(
                name: "RejeteAutomatiquement",
                table: "Besoins");

            migrationBuilder.AlterColumn<int>(
                name: "ValidateurId",
                table: "Validations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
