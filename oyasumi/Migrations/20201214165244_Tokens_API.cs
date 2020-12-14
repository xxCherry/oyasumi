using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace oyasumi.Migrations
{
    public partial class Tokens_API : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreferNightcore",
                table: "Users",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserpageContent",
                table: "Users",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserToken = table.Column<string>(nullable: true),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelaxStats");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "VanillaStats");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferNightcore",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserpageContent",
                table: "Users");

            migrationBuilder.AlterColumn<float>(
                name: "PerformancePoints",
                table: "Scores",
                type: "float",
                nullable: false,
                oldClrType: typeof(double));

            migrationBuilder.AlterColumn<float>(
                name: "Accuracy",
                table: "Scores",
                type: "float",
                nullable: false,
                oldClrType: typeof(double));

            migrationBuilder.CreateTable(
                name: "UserStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccuracyCtb = table.Column<float>(type: "float", nullable: false),
                    AccuracyMania = table.Column<float>(type: "float", nullable: false),
                    AccuracyOsu = table.Column<float>(type: "float", nullable: false),
                    AccuracyTaiko = table.Column<float>(type: "float", nullable: false),
                    PerformanceCtb = table.Column<int>(type: "int", nullable: false),
                    PerformanceMania = table.Column<int>(type: "int", nullable: false),
                    PerformanceOsu = table.Column<int>(type: "int", nullable: false),
                    PerformanceTaiko = table.Column<int>(type: "int", nullable: false),
                    PlaycountCtb = table.Column<int>(type: "int", nullable: false),
                    PlaycountMania = table.Column<int>(type: "int", nullable: false),
                    PlaycountOsu = table.Column<int>(type: "int", nullable: false),
                    PlaycountTaiko = table.Column<int>(type: "int", nullable: false),
                    RankCtb = table.Column<int>(type: "int", nullable: false),
                    RankMania = table.Column<int>(type: "int", nullable: false),
                    RankOsu = table.Column<int>(type: "int", nullable: false),
                    RankTaiko = table.Column<int>(type: "int", nullable: false),
                    RankedScoreCtb = table.Column<long>(type: "bigint", nullable: false),
                    RankedScoreMania = table.Column<long>(type: "bigint", nullable: false),
                    RankedScoreOsu = table.Column<long>(type: "bigint", nullable: false),
                    RankedScoreTaiko = table.Column<long>(type: "bigint", nullable: false),
                    TotalScoreCtb = table.Column<long>(type: "bigint", nullable: false),
                    TotalScoreMania = table.Column<long>(type: "bigint", nullable: false),
                    TotalScoreOsu = table.Column<long>(type: "bigint", nullable: false),
                    TotalScoreTaiko = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStats", x => x.Id);
                });
        }
    }
}
