using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NBAJam.Migrations
{
    /// <inheritdoc />
    public partial class init5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
            name: "WinningTeamId",
            table: "Tournaments",
            nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_WinningTeamId",
                table: "Tournaments",
                column: "WinningTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Teams_WinningTeamId",
                table: "Tournaments",
                column: "WinningTeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
            name: "FK_Tournaments_Teams_WinningTeamId",
            table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_WinningTeamId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "WinningTeamId",
                table: "Tournaments");
        }
    }
}
