using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nordly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewsletterConsentAndUnsubscribe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentGivenAt",
                table: "NewsletterSubscribers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentText",
                table: "NewsletterSubscribers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubscribed",
                table: "NewsletterSubscribers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UnsubscribeVersion",
                table: "NewsletterSubscribers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnsubscribedAt",
                table: "NewsletterSubscribers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentGivenAt",
                table: "NewsletterSubscribers");

            migrationBuilder.DropColumn(
                name: "ConsentText",
                table: "NewsletterSubscribers");

            migrationBuilder.DropColumn(
                name: "IsSubscribed",
                table: "NewsletterSubscribers");

            migrationBuilder.DropColumn(
                name: "UnsubscribeVersion",
                table: "NewsletterSubscribers");

            migrationBuilder.DropColumn(
                name: "UnsubscribedAt",
                table: "NewsletterSubscribers");
        }
    }
}
