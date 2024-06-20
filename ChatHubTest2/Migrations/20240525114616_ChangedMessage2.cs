using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatHubTest2.Migrations
{
    /// <inheritdoc />
    public partial class ChangedMessage2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecipientConnectionId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SenderConnectionId",
                table: "Messages");

            migrationBuilder.AddColumn<DateTime>(
                name: "SendTime",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SendTime",
                table: "Messages");

            migrationBuilder.AddColumn<string>(
                name: "RecipientConnectionId",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderConnectionId",
                table: "Messages",
                type: "text",
                nullable: true);
        }
    }
}
