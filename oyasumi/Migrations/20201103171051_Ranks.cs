using Microsoft.EntityFrameworkCore.Migrations;

namespace oyasumi.Migrations
{
    public partial class Ranks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RankCtb",
                table: "UserStats",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RankMania",
                table: "UserStats",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RankOsu",
                table: "UserStats",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RankTaiko",
                table: "UserStats",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RankCtb",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "RankMania",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "RankOsu",
                table: "UserStats");

            migrationBuilder.DropColumn(
                name: "RankTaiko",
                table: "UserStats");
        }
    }
}
