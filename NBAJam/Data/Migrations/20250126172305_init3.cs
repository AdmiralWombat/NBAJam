using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NBAJam.Data.Migrations
{
    /// <inheritdoc />
    public partial class init3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team1Points",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Team2Points",
                table: "Games");

            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "Teams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamPoints",
                table: "Games",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_GameId",
                table: "Teams",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Games_GameId",
                table: "Teams",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Games_GameId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_GameId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TeamPoints",
                table: "Games");

            migrationBuilder.AddColumn<int>(
                name: "Team1Points",
                table: "Games",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Team2Points",
                table: "Games",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
