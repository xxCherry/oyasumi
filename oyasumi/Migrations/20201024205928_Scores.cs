using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace oyasumi.Migrations
{
    public partial class Scores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Privileges",
                table: "Users",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Count100 = table.Column<int>(nullable: false),
                    Count300 = table.Column<int>(nullable: false),
                    Count50 = table.Column<int>(nullable: false),
                    CountGeki = table.Column<int>(nullable: false),
                    CountKatu = table.Column<int>(nullable: false),
                    CountMiss = table.Column<int>(nullable: false),
                    TotalScore = table.Column<int>(nullable: false),
                    Accuracy = table.Column<float>(nullable: false),
                    FileChecksum = table.Column<string>(nullable: true),
                    MaxCombo = table.Column<int>(nullable: false),
                    Passed = table.Column<bool>(nullable: false),
                    Mods = table.Column<int>(nullable: false),
                    PlayMode = table.Column<int>(nullable: false),
                    Flags = table.Column<int>(nullable: false),
                    OsuVersion = table.Column<int>(nullable: false),
                    Perfect = table.Column<bool>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false),
                    ReplayChecksum = table.Column<string>(nullable: true),
                    Relaxing = table.Column<bool>(nullable: false),
                    AutoPiloting = table.Column<bool>(nullable: false),
                    PerformancePoints = table.Column<float>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropColumn(
                name: "Privileges",
                table: "Users");
        }
    }
}
