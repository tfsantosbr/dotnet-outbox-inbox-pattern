using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Consumer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxMessageErrorTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "processed_on_utc",
                schema: "inventory",
                table: "inbox_messages",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "error_handled_on_utc",
                schema: "inventory",
                table: "inbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error",
                schema: "inventory",
                table: "inbox_messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "error_handled_on_utc",
                schema: "inventory",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "error",
                schema: "inventory",
                table: "inbox_messages");

            migrationBuilder.AlterColumn<DateTime>(
                name: "processed_on_utc",
                schema: "inventory",
                table: "inbox_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
