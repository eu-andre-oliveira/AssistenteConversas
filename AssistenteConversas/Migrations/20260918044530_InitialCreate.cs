using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistenteConversas.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Empresa = table.Column<string>(type: "TEXT", nullable: true),
                    Cargo = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Telefone = table.Column<string>(type: "TEXT", nullable: true),
                    PerfilLinkedIn = table.Column<string>(type: "TEXT", nullable: true),
                    Necessidade = table.Column<string>(type: "TEXT", nullable: true),
                    ProdutoInteresse = table.Column<string>(type: "TEXT", nullable: true),
                    NivelInteresse = table.Column<string>(type: "TEXT", nullable: true),
                    Temperatura = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PrazoEstimado = table.Column<string>(type: "TEXT", nullable: true),
                    ProximaAcao = table.Column<string>(type: "TEXT", nullable: true),
                    Resumo = table.Column<string>(type: "TEXT", nullable: true),
                    Observacoes = table.Column<string>(type: "TEXT", nullable: true),
                    ConversaOriginal = table.Column<string>(type: "TEXT", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Empresa",
                table: "Leads",
                column: "Empresa");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Nome",
                table: "Leads",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Status",
                table: "Leads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Temperatura",
                table: "Leads",
                column: "Temperatura");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Leads");
        }
    }
}
