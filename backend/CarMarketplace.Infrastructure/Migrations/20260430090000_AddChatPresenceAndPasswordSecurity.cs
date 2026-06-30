using CarMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarMarketplace.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260430090000_AddChatPresenceAndPasswordSecurity")]
    public partial class AddChatPresenceAndPasswordSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDelivered",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSeen",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SeenAt",
                table: "Messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeen",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValueSql: "CONVERT(nvarchar(36), NEWID())");

            migrationBuilder.Sql(
                """
                UPDATE [Messages]
                SET [IsSeen] = [IsRead],
                    [SeenAt] = CASE WHEN [IsRead] = CAST(1 AS bit) THEN [CreatedAt] ELSE NULL END,
                    [IsDelivered] = CASE WHEN [IsRead] = CAST(1 AS bit) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,
                    [DeliveredAt] = CASE WHEN [IsRead] = CAST(1 AS bit) THEN [CreatedAt] ELSE NULL END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId_IsDelivered",
                table: "Messages",
                columns: new[] { "ReceiverId", "IsDelivered" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId_SenderId_IsSeen",
                table: "Messages",
                columns: new[] { "ReceiverId", "SenderId", "IsSeen" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ReceiverId_IsDelivered",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ReceiverId_SenderId_IsSeen",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsDelivered",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsSeen",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SeenAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSeen",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Users");
        }
    }
}
