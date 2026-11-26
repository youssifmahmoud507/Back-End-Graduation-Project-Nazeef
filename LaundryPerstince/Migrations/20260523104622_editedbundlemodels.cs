using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryPerstince.Migrations
{
    /// <inheritdoc />
    public partial class editedbundlemodels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowCouponStacking",
                table: "Bundles");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BundleOrderMetadata",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "BundleOrderMetadata",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BundleOrderMetadata");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BundleOrderMetadata");

            migrationBuilder.AddColumn<bool>(
                name: "AllowCouponStacking",
                table: "Bundles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
