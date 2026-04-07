using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOST.Migrations
{
    /// <inheritdoc />
    public partial class AddPartyCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Parties",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_CurrentPartyId",
                table: "RestaurantTables",
                column: "CurrentPartyId");

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantTables_Parties_CurrentPartyId",
                table: "RestaurantTables",
                column: "CurrentPartyId",
                principalTable: "Parties",
                principalColumn: "PartyId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantTables_Parties_CurrentPartyId",
                table: "RestaurantTables");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_CurrentPartyId",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Parties");
        }
    }
}
