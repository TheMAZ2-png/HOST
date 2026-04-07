using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOST.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualWaitMinutes",
                table: "Parties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedWaitAtJoin",
                table: "Parties",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualWaitMinutes",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "EstimatedWaitAtJoin",
                table: "Parties");
        }
    }
}
