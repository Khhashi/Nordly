using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nordly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistDistributedCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DistributedCacheEntries",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Value = table.Column<byte[]>(type: "bytea", nullable: false),
                    AbsoluteExpiration = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SlidingExpiration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    LastAccessed = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributedCacheEntries", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistributedCacheEntries_AbsoluteExpiration",
                table: "DistributedCacheEntries",
                column: "AbsoluteExpiration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DistributedCacheEntries");
        }
    }
}
