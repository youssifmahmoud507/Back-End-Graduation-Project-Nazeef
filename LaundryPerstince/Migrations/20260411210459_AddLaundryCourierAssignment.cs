using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryPerstince.Migrations
{
    /// <inheritdoc />
    public partial class AddLaundryCourierAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LaundryId",
                table: "CourierProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "CommissionPayments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_CourierProfiles_LaundryId",
                table: "CourierProfiles",
                column: "LaundryId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierProfiles_Laundries_LaundryId",
                table: "CourierProfiles",
                column: "LaundryId",
                principalTable: "Laundries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierProfiles_Laundries_LaundryId",
                table: "CourierProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CourierProfiles_LaundryId",
                table: "CourierProfiles");

            migrationBuilder.DropColumn(
                name: "LaundryId",
                table: "CourierProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "CommissionPayments",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);
        }
    }
}
