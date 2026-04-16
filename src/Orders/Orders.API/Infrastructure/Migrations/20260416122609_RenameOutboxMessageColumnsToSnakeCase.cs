using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameOutboxMessageColumnsToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending",
                schema: "orders",
                table: "outbox_messages");

            migrationBuilder.RenameColumn(
                name: "Type",
                schema: "orders",
                table: "outbox_messages",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Headers",
                schema: "orders",
                table: "outbox_messages",
                newName: "headers");

            migrationBuilder.RenameColumn(
                name: "Error",
                schema: "orders",
                table: "outbox_messages",
                newName: "error");

            migrationBuilder.RenameColumn(
                name: "Destination",
                schema: "orders",
                table: "outbox_messages",
                newName: "destination");

            migrationBuilder.RenameColumn(
                name: "Content",
                schema: "orders",
                table: "outbox_messages",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "orders",
                table: "outbox_messages",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ProcessedOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "processed_on_utc");

            migrationBuilder.RenameColumn(
                name: "OccurredOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "occurred_on_utc");

            migrationBuilder.RenameColumn(
                name: "ErrorHandledOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "error_handled_on_utc");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_ProcessedOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "IX_outbox_messages_processed_on_utc");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_ErrorHandledOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "IX_outbox_messages_error_handled_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending",
                schema: "orders",
                table: "outbox_messages",
                column: "occurred_on_utc",
                filter: "\"processed_on_utc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending",
                schema: "orders",
                table: "outbox_messages");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "orders",
                table: "outbox_messages",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "headers",
                schema: "orders",
                table: "outbox_messages",
                newName: "Headers");

            migrationBuilder.RenameColumn(
                name: "error",
                schema: "orders",
                table: "outbox_messages",
                newName: "Error");

            migrationBuilder.RenameColumn(
                name: "destination",
                schema: "orders",
                table: "outbox_messages",
                newName: "Destination");

            migrationBuilder.RenameColumn(
                name: "content",
                schema: "orders",
                table: "outbox_messages",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "orders",
                table: "outbox_messages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "processed_on_utc",
                schema: "orders",
                table: "outbox_messages",
                newName: "ProcessedOnUtc");

            migrationBuilder.RenameColumn(
                name: "occurred_on_utc",
                schema: "orders",
                table: "outbox_messages",
                newName: "OccurredOnUtc");

            migrationBuilder.RenameColumn(
                name: "error_handled_on_utc",
                schema: "orders",
                table: "outbox_messages",
                newName: "ErrorHandledOnUtc");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_processed_on_utc",
                schema: "orders",
                table: "outbox_messages",
                newName: "IX_outbox_messages_ProcessedOnUtc");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_error_handled_on_utc",
                schema: "orders",
                table: "outbox_messages",
                newName: "IX_outbox_messages_ErrorHandledOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending",
                schema: "orders",
                table: "outbox_messages",
                column: "OccurredOnUtc",
                filter: "\"ProcessedOnUtc\" IS NULL");
        }
    }
}