using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameOutboxMessageDateColumnsToUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProcessedOn",
                schema: "orders",
                table: "outbox_messages",
                newName: "ProcessedOnUtc");

            migrationBuilder.RenameColumn(
                name: "OccurredOn",
                schema: "orders",
                table: "outbox_messages",
                newName: "OccurredOnUtc");

            migrationBuilder.RenameColumn(
                name: "ErrorHandledOn",
                schema: "orders",
                table: "outbox_messages",
                newName: "ErrorHandledOnUtc");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "orders",
                table: "outbox_messages",
                newName: "IX_outbox_messages_ProcessedOnUtc");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_OccurredOn",
                schema: "orders",
                table: "outbox_messages",
                newName: "IX_outbox_messages_OccurredOnUtc");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_ErrorHandledOn",
                schema: "orders",
                table: "outbox_messages",
                newName: "IX_outbox_messages_ErrorHandledOnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProcessedOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "ProcessedOn");

            migrationBuilder.RenameColumn(
                name: "OccurredOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "OccurredOn");

            migrationBuilder.RenameColumn(
                name: "ErrorHandledOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "ErrorHandledOn");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_ProcessedOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "IX_outbox_messages_ProcessedOn");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_OccurredOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "IX_outbox_messages_OccurredOn");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_ErrorHandledOnUtc",
                schema: "orders",
                table: "outbox_messages",
                newName: "IX_outbox_messages_ErrorHandledOn");
        }
    }
}