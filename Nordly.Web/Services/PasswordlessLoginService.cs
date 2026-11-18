using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Nordly.Infrastructure.Data;

namespace Nordly.Web.Services;

public enum PasswordlessLoginRequestStatus
{
    Sent,
    Throttled,
    InvalidEmail,
    EmailUnavailable
}

public sealed class PasswordlessLoginService(
    OrderDbContext db,
    UserManager<CustomerAccount> userManager,
    IPasswordHasher<CustomerAccount> passwordHasher,
    IPasswordlessLoginEmailSender emailSender,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);
    private const int MaximumFailedAttempts = 5;

    public async Task<PasswordlessLoginRequestStatus> RequestCodeAsync(
        string? email, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeEmail(email, out var normalizedEmail, out var trimmedEmail))
            return PasswordlessLoginRequestStatus.InvalidEmail;

        var now = timeProvider.GetUtcNow();
        var code = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var codeHash = passwordHasher.HashPassword(new CustomerAccount(), code);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var challenge = await FindChallengeForUpdateAsync(normalizedEmail, cancellationToken);
        if (challenge is not null && challenge.CreatedAt + ResendCooldown > now)
        {
            await transaction.CommitAsync(cancellationToken);
            return PasswordlessLoginRequestStatus.Throttled;
        }

        if (challenge is null)
        {
            challenge = new PasswordlessLoginChallenge { NormalizedEmail = normalizedEmail };
            db.PasswordlessLoginChallenges.Add(challenge);
        }

        challenge.CodeHash = codeHash;
        challenge.CreatedAt = now;
        challenge.ExpiresAt = now + CodeLifetime;
        challenge.FailedAttempts = 0;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return PasswordlessLoginRequestStatus.Throttled;
        }

        if (await emailSender.SendAsync(trimmedEmail, code, cancellationToken))
            return PasswordlessLoginRequestStatus.Sent;

        await db.PasswordlessLoginChallenges
            .Where(item => item.NormalizedEmail == normalizedEmail && item.CodeHash == codeHash)
            .ExecuteDeleteAsync(cancellationToken);
        return PasswordlessLoginRequestStatus.EmailUnavailable;
    }

    public async Task<CustomerAccount?> VerifyCodeAsync(
        string? email, string? code, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeEmail(email, out var normalizedEmail, out var trimmedEmail))
            return null;

        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var challenge = await FindChallengeForUpdateAsync(normalizedEmail, cancellationToken);
        if (challenge is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        if (challenge.ExpiresAt <= now || challenge.FailedAttempts >= MaximumFailedAttempts)
        {
            db.PasswordlessLoginChallenges.Remove(challenge);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        challenge.FailedAttempts++;
        var matches = code is { Length: 6 }
                      && code.All(char.IsAsciiDigit)
                      && passwordHasher.VerifyHashedPassword(new CustomerAccount(), challenge.CodeHash, code)
                      is not PasswordVerificationResult.Failed;

        if (!matches)
        {
            if (challenge.FailedAttempts >= MaximumFailedAttempts)
                db.PasswordlessLoginChallenges.Remove(challenge);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        db.PasswordlessLoginChallenges.Remove(challenge);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var user = await userManager.FindByEmailAsync(trimmedEmail);
        if (user is null)
        {
            user = new CustomerAccount
            {
                UserName = trimmedEmail,
                Email = trimmedEmail,
                EmailConfirmed = true,
                CreatedAt = now
            };
            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                user = await userManager.FindByEmailAsync(trimmedEmail);
                if (user is null)
                    throw new InvalidOperationException(
                        $"Could not create a verified customer account ({string.Join(", ", createResult.Errors.Select(error => error.Code))}).");
            }
        }
        else if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException(
                    $"Could not confirm a verified customer email ({string.Join(", ", updateResult.Errors.Select(error => error.Code))}).");
        }

        return user;
    }

    private async Task<PasswordlessLoginChallenge?> FindChallengeForUpdateAsync(
        string normalizedEmail, CancellationToken cancellationToken)
    {
        return await db.PasswordlessLoginChallenges
            .FromSqlInterpolated(
                $"SELECT * FROM \"PasswordlessLoginChallenges\" WHERE \"NormalizedEmail\" = {normalizedEmail} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    private bool TryNormalizeEmail(string? email, out string normalizedEmail, out string trimmedEmail)
    {
        trimmedEmail = email?.Trim() ?? string.Empty;
        normalizedEmail = string.Empty;
        if (trimmedEmail.Length is 0 or > 256 || !new EmailAddressAttribute().IsValid(trimmedEmail))
            return false;

        normalizedEmail = userManager.NormalizeEmail(trimmedEmail) ?? string.Empty;
        return normalizedEmail.Length > 0;
    }
}
