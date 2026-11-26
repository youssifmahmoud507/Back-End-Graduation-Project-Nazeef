using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryPerstince.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityVerificationColumnToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIdentityVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIdentityVerified",
                table: "Users");
        }
    }
}
