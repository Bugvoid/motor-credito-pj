using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "credit_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    rating = table.Column<string>(type: "text", nullable: false),
                    spread_annual = table.Column<decimal>(type: "numeric(12,8)", precision: 12, scale: 8, nullable: true),
                    annual_rate = table.Column<decimal>(type: "numeric(12,8)", precision: 12, scale: 8, nullable: true),
                    monthly_rate = table.Column<decimal>(type: "numeric(12,8)", precision: 12, scale: 8, nullable: true),
                    cet_annual = table.Column<decimal>(type: "numeric(12,8)", precision: 12, scale: 8, nullable: true),
                    iof = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    policy_version = table.Column<string>(type: "text", nullable: false),
                    rule_results = table.Column<string>(type: "jsonb", nullable: false),
                    schedule = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_decisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_credit_decisions_credit_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "credit_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_credit_decisions_request_id",
                table: "credit_decisions",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_created_at",
                table: "outbox_messages",
                column: "created_at",
                filter: "sent_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_decisions");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "credit_requests");
        }
    }
}
