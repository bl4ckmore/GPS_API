// D:\GPS_HUB\GPS_API\ECommerceApp.Infrastructure\Migrations\20251105193211_AddOrderNumberToOrder.cs

using System; // Ensure System is included for DateTime/Guid types
using Microsoft.EntityFrameworkCore.Migrations; // <--- CRITICAL FIX: Ensures MigrationBuilder is found

#nullable disable

namespace ECommerceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumberToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Index safety drops (Addresses previous 42704 errors) ---
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_OrderItems_OrderId_ProductId\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_OrderItems_ProductId\";");

            // --- Foreign Key Constraint safety drop (Addresses previous 42710 error) ---
            migrationBuilder.Sql("ALTER TABLE \"OrderItems\" DROP CONSTRAINT IF EXISTS \"FK_OrderItems_Orders_OrderId\";");

            // --- Add the new OrderNumber column ---
            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "text",
                nullable: true);

            // --- Add the Foreign Key constraint (Re-adds the correct FK) ---
            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "Orders");

            // Re-create the indexes that were dropped in Up() 
            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ProductId",
                table: "OrderItems",
                columns: new[] { "OrderId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");
        }
    }
}