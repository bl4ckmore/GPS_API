using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Update_20250825 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.RenameTable(
                name: "UserSession",
                newName: "UserSession",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "ShippingAddress",
                newName: "ShippingAddress",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "ProductSEO",
                newName: "ProductSEO",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "Products",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "ProductImage",
                newName: "ProductImage",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "ProductAttribute",
                newName: "ProductAttribute",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "PaymentInfo",
                newName: "PaymentInfo",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "Orders",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "OrderItems",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Category",
                newName: "Category",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Carts",
                newName: "Carts",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "CartItems",
                newName: "CartItems",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "BillingAddress",
                newName: "BillingAddress",
                newSchema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "UserSession",
                schema: "public",
                newName: "UserSession");

            migrationBuilder.RenameTable(
                name: "ShippingAddress",
                schema: "public",
                newName: "ShippingAddress");

            migrationBuilder.RenameTable(
                name: "ProductSEO",
                schema: "public",
                newName: "ProductSEO");

            migrationBuilder.RenameTable(
                name: "Products",
                schema: "public",
                newName: "Products");

            migrationBuilder.RenameTable(
                name: "ProductImage",
                schema: "public",
                newName: "ProductImage");

            migrationBuilder.RenameTable(
                name: "ProductAttribute",
                schema: "public",
                newName: "ProductAttribute");

            migrationBuilder.RenameTable(
                name: "PaymentInfo",
                schema: "public",
                newName: "PaymentInfo");

            migrationBuilder.RenameTable(
                name: "Orders",
                schema: "public",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                schema: "public",
                newName: "OrderItems");

            migrationBuilder.RenameTable(
                name: "Category",
                schema: "public",
                newName: "Category");

            migrationBuilder.RenameTable(
                name: "Carts",
                schema: "public",
                newName: "Carts");

            migrationBuilder.RenameTable(
                name: "CartItems",
                schema: "public",
                newName: "CartItems");

            migrationBuilder.RenameTable(
                name: "BillingAddress",
                schema: "public",
                newName: "BillingAddress");
        }
    }
}
