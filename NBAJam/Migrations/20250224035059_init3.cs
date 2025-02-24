using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NBAJam.Migrations
{
    /// <inheritdoc />
    public partial class init3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team1Points",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Team2Points",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "TeamPoints",
                table: "Games",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
