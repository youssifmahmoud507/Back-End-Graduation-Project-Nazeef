using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryPerstince.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommissionPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LaundryId = table.Column<int>(type: "int", nullable: false),
                    LaundryAdminId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MerchantOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionPayments_Laundries_LaundryId",
                        column: x => x.LaundryId,
                        principalTable: "Laundries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommissionPayments_Users_LaundryAdminId",
                        column: x => x.LaundryAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionPayments_LaundryAdminId",
                table: "CommissionPayments",
                column: "LaundryAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionPayments_LaundryId",
                table: "CommissionPayments",
                column: "LaundryId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionPayments_MerchantOrderId",
                table: "CommissionPayments",
                column: "MerchantOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionPayments");
        }
    }
}
