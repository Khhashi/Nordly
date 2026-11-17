using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;

namespace Nordly.Tests;

[TestFixture]
public sealed class PasswordlessLoginServiceTests
{
    private readonly string? _migrationConnection = Environment.GetEnvironmentVariable("NORDLY_TEST_MIGRATION_CONNECTION");

    private sealed class CapturingEmailSender : IPasswordlessLoginEmailSender
    {
        public string? Code { get; private set; }
        public int SentCount { get; private set; }

        public Task<bool> SendAsync(string email, string code, CancellationToken cancellationToken = default)
        {
            Code = code;
            SentCount++;
            return Task.FromResult(true);
        }
    }

    [Test]
    public async Task Verified_Code_Creates_Confirmed_Account_And_Cannot_Be_Reused()
    {
        await InDatabase(async (service, db, sender) =>
        {
            const string email = "customer@example.test";
            var requestResult = await service.RequestCodeAsync(email);

            Assert.That(requestResult, Is.EqualTo(PasswordlessLoginRequestStatus.Sent));
            Assert.That(sender.SentCount, Is.EqualTo(1));
            Assert.That(sender.Code, Does.Match("^[0-9]{6}$"));
            Assert.That(await db.Users.CountAsync(), Is.Zero);

            var user = await service.VerifyCodeAsync(email.ToUpperInvariant(), sender.Code);

            Assert.That(user, Is.Not.Null);
            Assert.That(user!.Email, Is.EqualTo(email.ToUpperInvariant()));
            Assert.That(user.EmailConfirmed, Is.True);
            Assert.That(await db.Users.CountAsync(), Is.EqualTo(1));
            Assert.That(await service.VerifyCodeAsync(email, sender.Code), Is.Null);
        });
    }

    [Test]
    public async Task Request_Is_Throttled_During_The_Resend_Cooldown()
    {
        await InDatabase(async (service, _, sender) =>
        {
            const string email = "customer@example.test";

            var first = await service.RequestCodeAsync(email);
            var second = await service.RequestCodeAsync(email);

            Assert.That(first, Is.EqualTo(PasswordlessLoginRequestStatus.Sent));
            Assert.That(second, Is.EqualTo(PasswordlessLoginRequestStatus.Throttled));
            Assert.That(sender.SentCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Expired_Code_Is_Rejected_Without_Creating_An_Account()
    {
        await InDatabase(async (service, db, sender) =>
        {
            const string email = "customer@example.test";
            await service.RequestCodeAsync(email);

            var challenge = await db.PasswordlessLoginChallenges.SingleAsync();
            challenge.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();

            Assert.That(await service.VerifyCodeAsync(email, sender.Code), Is.Null);
            Assert.That(await db.Users.CountAsync(), Is.Zero);
            Assert.That(await db.PasswordlessLoginChallenges.CountAsync(), Is.Zero);
        });
    }

    [Test]
    public async Task Five_Invalid_Codes_Consume_The_Challenge()
    {
        await InDatabase(async (service, db, sender) =>
        {
            const string email = "customer@example.test";
            await service.RequestCodeAsync(email);
            var invalidCode = sender.Code == "000000" ? "000001" : "000000";

            for (var attempt = 0; attempt < 5; attempt++)
                Assert.That(await service.VerifyCodeAsync(email, invalidCode), Is.Null);

            Assert.That(await service.VerifyCodeAsync(email, sender.Code), Is.Null);
            Assert.That(await db.Users.CountAsync(), Is.Zero);
            Assert.That(await db.PasswordlessLoginChallenges.CountAsync(), Is.Zero);
        });
    }

    private async Task InDatabase(Func<PasswordlessLoginService, OrderDbContext, CapturingEmailSender, Task> test)
    {
        if (string.IsNullOrWhiteSpace(_migrationConnection))
            Assert.Ignore("Set NORDLY_TEST_MIGRATION_CONNECTION to run PostgreSQL login integration tests.");

        await DatabaseMigrationTests.InIsolatedSchema(async connectionString =>
        {
            var schema = new NpgsqlConnectionStringBuilder(connectionString).SearchPath;
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseNpgsql(connectionString, postgres =>
                    postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .Options;

            await using (var migrationDb = new OrderDbContext(options))
                await migrationDb.Database.MigrateAsync();

            var sender = new CapturingEmailSender();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<OrderDbContext>(dbOptions => dbOptions.UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema)));
            services.AddIdentityCore<CustomerAccount>(identityOptions =>
                identityOptions.User.RequireUniqueEmail = true)
                .AddEntityFrameworkStores<OrderDbContext>();
            services.AddSingleton<IPasswordlessLoginEmailSender>(sender);
            services.AddSingleton<TimeProvider>(TimeProvider.System);

            using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;
            var service = new PasswordlessLoginService(
                scopedProvider.GetRequiredService<OrderDbContext>(),
                scopedProvider.GetRequiredService<UserManager<CustomerAccount>>(),
                scopedProvider.GetRequiredService<IPasswordHasher<CustomerAccount>>(),
                sender,
                TimeProvider.System);

            await test(service, scopedProvider.GetRequiredService<OrderDbContext>(), sender);
        });
    }
}
