using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkshopManager.Migrations
{
    /// <inheritdoc />
    public partial class AddNonClusteredIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_CreatedAt",
                table: "ServiceOrders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_Status",
                table: "ServiceOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_Status_CreatedAt",
                table: "ServiceOrders",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Parts_Category",
                table: "Parts",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_Name",
                table: "Parts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_CreatedAt",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_Status",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_Status_CreatedAt",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_Parts_Category",
                table: "Parts");

            migrationBuilder.DropIndex(
                name: "IX_Parts_Name",
                table: "Parts");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");
        }
    }
}
