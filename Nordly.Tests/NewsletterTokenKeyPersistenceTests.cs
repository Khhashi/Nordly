using Microsoft.AspNetCore.DataProtection;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterTokenKeyPersistenceTests
{
    [Test]
    public void Token_Can_Be_Read_After_Provider_Restart_With_Persisted_Keys()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"nordly-newsletter-{Guid.NewGuid():N}");
        try
        {
            var keys = new DirectoryInfo(directory);
            var first = new NewsletterUnsubscribeToken(DataProtectionProvider.Create(keys));
            var subscriberId = Guid.NewGuid();
            var version = Guid.NewGuid();
            var token = first.Create(subscriberId, version);

            var restarted = new NewsletterUnsubscribeToken(DataProtectionProvider.Create(keys));
            Assert.That(restarted.TryRead(token, out var savedId, out var savedVersion), Is.True);
            Assert.That(savedId, Is.EqualTo(subscriberId));
            Assert.That(savedVersion, Is.EqualTo(version));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
