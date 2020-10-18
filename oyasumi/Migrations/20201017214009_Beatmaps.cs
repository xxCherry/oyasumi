using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace oyasumi.Migrations
{
    public partial class Beatmaps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Beatmaps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BeatmapMd5 = table.Column<string>(nullable: true),
                    BeatmapId = table.Column<int>(nullable: false),
                    BeatmapSetId = table.Column<int>(nullable: false),
                    Status = table.Column<sbyte>(nullable: false),
                    Frozen = table.Column<bool>(nullable: false),
                    PlayCount = table.Column<int>(nullable: false),
                    PassCount = table.Column<int>(nullable: false),
                    Artist = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    DifficultyName = table.Column<string>(nullable: true),
                    Creator = table.Column<string>(nullable: true),
                    BPM = table.Column<float>(nullable: false),
                    CircleSize = table.Column<float>(nullable: false),
                    OverallDifficulty = table.Column<float>(nullable: false),
                    ApproachRate = table.Column<float>(nullable: false),
                    HPDrainRate = table.Column<float>(nullable: false),
                    Stars = table.Column<float>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beatmaps", x => x.Id);
                });

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beatmaps");
        }
    }
}
