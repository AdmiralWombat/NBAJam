using Microsoft.EntityFrameworkCore.Migrations;

namespace NBAJam.Migrations
{
    public partial class UpdateGameModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing relationship if necessary
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Games_Teams_TeamsId",
            //    table: "Games");

            // Add new columns for Team1 and Team2
            migrationBuilder.AddColumn<int>(
                name: "Team1TeamId",
                table: "Games",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Team2TeamId",
                table: "Games",
                nullable: true);

            // Update existing data to ensure data integrity
            // Assuming that the existing Teams list is ordered and the first two teams are Team1 and Team2
            migrationBuilder.Sql(
                @"
            UPDATE Games
            SET Team1TeamId = (SELECT TOP 1 TeamId FROM Teams WHERE Teams.GameId = Games.GameId ORDER BY Teams.TeamId),
                Team2TeamId = (SELECT TOP 1 TeamId FROM Teams WHERE Teams.GameId = Games.GameId ORDER BY Teams.TeamId OFFSET 1 ROWS)
            ");

            // Add foreign key constraints for Team1 and Team2
            migrationBuilder.AddForeignKey(
                name: "FK_Games_Teams_Team1TeamId",
                table: "Games",
                column: "Team1TeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Teams_Team2TeamId",
                table: "Games",
                column: "Team2TeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.NoAction); // Specify NO ACTION to avoid multiple cascade paths
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove foreign key constraints
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Teams_Team1TeamId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_Teams_Team2TeamId",
                table: "Games");

            // Drop new columns
            migrationBuilder.DropColumn(
                name: "Team1TeamId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Team2TeamId",
                table: "Games");

            // Re-add previous relationship if necessary
            /*migrationBuilder.AddForeignKey(
                name: "FK_Games_Teams_TeamsId",
                table: "Games",
                column: "TeamsId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Cascade);*/
        }
    }
}
