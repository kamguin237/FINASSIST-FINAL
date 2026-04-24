using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAgentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Besoins_Utilisateurs_AgentId",
                table: "Besoins");

            migrationBuilder.DropIndex(
                name: "IX_Besoins_AgentId",
                table: "Besoins");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Besoins");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgentId",
                table: "Besoins",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Besoins_AgentId",
                table: "Besoins",
                column: "AgentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Besoins_Utilisateurs_AgentId",
                table: "Besoins",
                column: "AgentId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
