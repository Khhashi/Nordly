using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nordly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributedCacheExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DistributedCacheEntries_AbsoluteExpiration",
                table: "DistributedCacheEntries");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "DistributedCacheEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "DistributedCacheEntries"
                SET "ExpiresAt" = CASE
                    WHEN "AbsoluteExpiration" IS NOT NULL AND "SlidingExpiration" IS NOT NULL
                        THEN LEAST("AbsoluteExpiration", "LastAccessed" + "SlidingExpiration")
                    WHEN "AbsoluteExpiration" IS NOT NULL THEN "AbsoluteExpiration"
                    WHEN "SlidingExpiration" IS NOT NULL THEN "LastAccessed" + "SlidingExpiration"
                    ELSE NULL
                END
                WHERE "AbsoluteExpiration" IS NOT NULL OR "SlidingExpiration" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DistributedCacheEntries_ExpiresAt",
                table: "DistributedCacheEntries",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DistributedCacheEntries_ExpiresAt",
                table: "DistributedCacheEntries");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "DistributedCacheEntries");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedCacheEntries_AbsoluteExpiration",
                table: "DistributedCacheEntries",
                column: "AbsoluteExpiration");
        }
    }
}
