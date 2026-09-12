using PwM.Mobile.Models;
using PwM.Mobile.Services;

namespace PwM.Tests.Encryption;

public class VaultSessionTests
{
    [Fact]
    public void Background_lock_invalidates_an_unlock_still_in_progress()
    {
        var session = new VaultSession();
        var pendingVersion = session.Version;
        session.Lock();

        Assert.False(session.TryUnlock(pendingVersion, "vault", "password"));
        Assert.False(session.IsUnlocked);
    }

    [Fact]
    public void A_previous_session_cannot_change_the_password_after_reopening_the_same_vault()
    {
        var session = new VaultSession();
        session.Unlock("vault", "old password");
        var version = session.Version;
        session.Lock();
        session.Unlock("vault", "current password");

        Assert.False(session.TryUpdateMasterPassword(version, "stale password"));
        Assert.False(session.IsCurrent(version));
        Assert.Equal("current password", session.MasterPassword);
    }

    [Fact]
    public void Lock_clears_prefetched_passwords_before_notifying_consumers()
    {
        var entry = new CredentialEntry { Password = "secret" };
        var session = new VaultSession();
        session.Unlock("vault", "master", [entry]);
        bool notified = false;
        session.Locked += (_, _) =>
        {
            notified = true;
            Assert.False(session.IsUnlocked);
            Assert.Empty(session.MasterPassword);
            Assert.Empty(entry.Password);
        };

        session.Lock();

        Assert.True(notified);
        Assert.Null(session.TakePrefetchedCredentials("vault"));
    }

    [Fact]
    public void Successful_unlock_hands_credentials_to_the_active_session_only_once()
    {
        var session = new VaultSession();
        var entry = new CredentialEntry { Password = "secret" };
        Assert.True(session.TryUnlock(session.Version, "vault", "master", [entry]));
        Assert.Null(session.TakePrefetchedCredentials("another vault"));
        Assert.Same(entry, Assert.Single(session.TakePrefetchedCredentials("vault")!));
        Assert.Equal("secret", entry.Password);
        Assert.Null(session.TakePrefetchedCredentials("vault"));
        Assert.True(session.TryUpdateMasterPassword(session.Version, "new master"));
        Assert.Equal("new master", session.MasterPassword);
    }
}
