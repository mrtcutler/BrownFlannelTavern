using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrownFlannelTavernStore.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailLogDeliveryStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryUpdatedAt",
                table: "EmailLogs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryUpdatedAt",
                table: "EmailLogs");
        }
    }
}
