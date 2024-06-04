using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatHubTest2.Migrations
{
    /// <inheritdoc />
    public partial class ChangedStatusInMessage2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StatusOfRead",
                table: "Messages",
                newName: "StatusRecipient");

            migrationBuilder.AddColumn<int>(
                name: "SenderStatus",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenderStatus",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "StatusRecipient",
                table: "Messages",
                newName: "StatusOfRead");
        }
    }
}
