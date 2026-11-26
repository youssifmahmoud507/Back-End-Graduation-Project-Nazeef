using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryPerstince.Migrations
{
    /// <inheritdoc />
    public partial class AddSentimentToReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Sentiment",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "SentimentScore",
                table: "Reviews",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SentimentSummary",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sentiment",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SentimentScore",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SentimentSummary",
                table: "Reviews");
        }
    }
}
