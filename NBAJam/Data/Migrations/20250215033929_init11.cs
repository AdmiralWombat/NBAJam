using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NBAJam.Data.Migrations
{
    /// <inheritdoc />
    public partial class init11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoundId",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Round",
                columns: table => new
                {
                    RoundId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoundNumber = table.Column<int>(type: "int", nullable: false),
                    TournamentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Round", x => x.RoundId);
                    table.ForeignKey(
                        name: "FK_Round_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "TournamentId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_RoundId",
                table: "Games",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Round_TournamentId",
                table: "Round",
                column: "TournamentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Round_RoundId",
                table: "Games",
                column: "RoundId",
                principalTable: "Round",
                principalColumn: "RoundId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Round_RoundId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "Round");

            migrationBuilder.DropIndex(
                name: "IX_Games_RoundId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "RoundId",
                table: "Games");
        }
    }
}
