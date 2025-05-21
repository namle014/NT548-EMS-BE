using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OA.Infrastructure.EF.Migrations
{
    /// <inheritdoc />
    public partial class doneholidayremove2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Done",
                table: "Holiday");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Done",
                table: "Holiday",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
