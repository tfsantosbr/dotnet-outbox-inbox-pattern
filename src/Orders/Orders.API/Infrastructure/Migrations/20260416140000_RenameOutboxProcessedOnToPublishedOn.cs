using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameOutboxProcessedOnToPublishedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_processed_on_utc",
                schema: "orders",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending",
                schema: "orders",
                table: "outbox_messages");

            migrationBuilder.RenameColumn(
                name: "processed_on_utc",
                schema: "orders",
                table: "outbox_messages",
                newName: "published_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_published_on_utc",
                schema: "orders",
                table: "outbox_messages",
                column: "published_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending",
                schema: "orders",
                table: "outbox_messages",
                column: "occurred_on_utc",
                filter: "\"published_on_utc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_published_on_utc",
                schema: "orders",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending",
                schema: "orders",
                table: "outbox_messages");

            migrationBuilder.RenameColumn(
                name: "published_on_utc",
                schema: "orders",
                table: "outbox_messages",
                newName: "processed_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc",
                schema: "orders",
                table: "outbox_messages",
                column: "processed_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending",
                schema: "orders",
                table: "outbox_messages",
                column: "occurred_on_utc",
                filter: "\"processed_on_utc\" IS NULL");
        }
    }
}
