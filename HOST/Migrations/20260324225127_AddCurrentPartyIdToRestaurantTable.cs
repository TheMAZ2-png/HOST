using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOST.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentPartyIdToRestaurantTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "Seatings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId1",
                table: "Seatings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentPartyId",
                table: "RestaurantTables",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Parties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Seatings_EmployeeId",
                table: "Seatings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Seatings_EmployeeId1",
                table: "Seatings",
                column: "EmployeeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Seatings_Employees_EmployeeId",
                table: "Seatings",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Seatings_Employees_EmployeeId1",
                table: "Seatings",
                column: "EmployeeId1",
                principalTable: "Employees",
                principalColumn: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seatings_Employees_EmployeeId",
                table: "Seatings");

            migrationBuilder.DropForeignKey(
                name: "FK_Seatings_Employees_EmployeeId1",
                table: "Seatings");

            migrationBuilder.DropIndex(
                name: "IX_Seatings_EmployeeId",
                table: "Seatings");

            migrationBuilder.DropIndex(
                name: "IX_Seatings_EmployeeId1",
                table: "Seatings");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Seatings");

            migrationBuilder.DropColumn(
                name: "EmployeeId1",
                table: "Seatings");

            migrationBuilder.DropColumn(
                name: "CurrentPartyId",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Parties");
        }
    }
}
