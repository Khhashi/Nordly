using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nordly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordlessLoginChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordlessLoginChallenges",
                columns: table => new
                {
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordlessLoginChallenges", x => x.NormalizedEmail);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordlessLoginChallenges_ExpiresAt",
                table: "PasswordlessLoginChallenges",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordlessLoginChallenges");
        }
    }
}
