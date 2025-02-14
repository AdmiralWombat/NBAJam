using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NBAJam.Data.Migrations
{
    /// <inheritdoc />
    public partial class init8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ByeTeam",
                table: "Teams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "TeamId", "ByeTeam", "GameId", "TournamentsWon" },
                values: new object[] { 1, true, null, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "ByeTeam",
                table: "Teams");
        }
    }
}
