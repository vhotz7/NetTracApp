using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetTracApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Vendor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HostName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssetTag = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PartID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FutureLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateReceived = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BackOrdered = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ready = table.Column<bool>(type: "bit", nullable: false),
                    LegacyDevice = table.Column<bool>(type: "bit", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryItems");
        }
    }
}
