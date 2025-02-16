using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NBAJam.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Team1TeamId",
                table: "Games",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Team2TeamId",
                table: "Games",
                nullable: true);



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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
