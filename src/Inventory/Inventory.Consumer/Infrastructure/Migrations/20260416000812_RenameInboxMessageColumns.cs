using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Consumer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameInboxMessageColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Consumer",
                schema: "inventory",
                table: "inbox_messages",
                newName: "consumer");

            migrationBuilder.RenameColumn(
                name: "ProcessedOnUtc",
                schema: "inventory",
                table: "inbox_messages",
                newName: "processed_on_utc");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                schema: "inventory",
                table: "inbox_messages",
                newName: "message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "consumer",
                schema: "inventory",
                table: "inbox_messages",
                newName: "Consumer");

            migrationBuilder.RenameColumn(
                name: "processed_on_utc",
                schema: "inventory",
                table: "inbox_messages",
                newName: "ProcessedOnUtc");

            migrationBuilder.RenameColumn(
                name: "message_id",
                schema: "inventory",
                table: "inbox_messages",
                newName: "MessageId");
        }
    }
}
