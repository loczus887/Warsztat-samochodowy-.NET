using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddNonClusteredIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Indeks dla wyszukiwania klientów po telefonie
        migrationBuilder.CreateIndex(
            name: "IX_Customers_PhoneNumber",
            table: "Customers",
            column: "PhoneNumber");

        // Indeks dla wyszukiwania pojazdów po numerze rejestracyjnym
        migrationBuilder.CreateIndex(
            name: "IX_Vehicles_RegistrationNumber",
            table: "Vehicles",
            column: "RegistrationNumber");

        // Indeks dla wyszukiwania zleceń po statusie
        migrationBuilder.CreateIndex(
            name: "IX_ServiceOrders_Status",
            table: "ServiceOrders",
            column: "Status");

        // Indeks dla wyszukiwania części po nazwie
        migrationBuilder.CreateIndex(
            name: "IX_Parts_Name",
            table: "Parts",
            column: "Name");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Customers_PhoneNumber",
            table: "Customers");

        migrationBuilder.DropIndex(
            name: "IX_Vehicles_RegistrationNumber",
            table: "Vehicles");

        migrationBuilder.DropIndex(
            name: "IX_ServiceOrders_Status",
            table: "ServiceOrders");

        migrationBuilder.DropIndex(
            name: "IX_Parts_Name",
            table: "Parts");
    }
}