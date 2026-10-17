using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteConversas.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowUpEInteracoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ProximoFollowUp",
                table: "Leads",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Interacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interacoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ProximoFollowUp",
                table: "Leads",
                column: "ProximoFollowUp");

            migrationBuilder.CreateIndex(
                name: "IX_Interacoes_DataRegistro",
                table: "Interacoes",
                column: "DataRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_Interacoes_LeadId",
                table: "Interacoes",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Interacoes");

            migrationBuilder.DropIndex(
                name: "IX_Leads_ProximoFollowUp",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ProximoFollowUp",
                table: "Leads");
        }
    }
}
