using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Headers = table.Column<string>(type: "jsonb", nullable: true),
                    Type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Destination = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorHandledOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ErrorHandledOn",
                schema: "orders",
                table: "outbox_messages",
                column: "ErrorHandledOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OccurredOn",
                schema: "orders",
                table: "outbox_messages",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "orders",
                table: "outbox_messages",
                column: "ProcessedOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "orders");
        }
    }
}
