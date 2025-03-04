using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NBAJam.Migrations
{
    /// <inheritdoc />
    public partial class init6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamTournaments_Tournaments_TournamentId1",
                table: "TeamTournaments");

            migrationBuilder.DropIndex(
                name: "IX_TeamTournaments_TournamentId1",
                table: "TeamTournaments");

            migrationBuilder.DropColumn(
                name: "TournamentId1",
                table: "TeamTournaments");

            migrationBuilder.AddColumn<int>(
                name: "WinningTeamId",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WinningTeamId",
                table: "Tournaments");

            migrationBuilder.AddColumn<int>(
                name: "TournamentId1",
                table: "TeamTournaments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamTournaments_TournamentId1",
                table: "TeamTournaments",
                column: "TournamentId1",
                unique: true,
                filter: "[TournamentId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamTournaments_Tournaments_TournamentId1",
                table: "TeamTournaments",
                column: "TournamentId1",
                principalTable: "Tournaments",
                principalColumn: "TournamentId");
        }
    }
}
