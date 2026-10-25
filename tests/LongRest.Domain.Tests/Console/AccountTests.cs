using LongRest.ConsoleApp;

namespace LongRest.Domain.Tests.Console;

public class AccountTests
{
    [Test]
    public async Task RegistrationAndLoginUseTrimmedCaseInsensitiveNames()
    {
        var accounts = new UserAccounts();
        var user = accounts.Register(" Alice ", "secret");
        await Assert.That(accounts.Login("alice", "secret")).IsEqualTo(user);
        await Assert.That(() => accounts.Register("ALICE", "different")).ThrowsExactly<InvalidOperationException>();
        await Assert.That(accounts.Users.Count).IsEqualTo(1);
        await Assert.That(() => accounts.Login("Alice", "wrong")).ThrowsExactly<InvalidOperationException>();
        await Assert.That(() => accounts.Login("nobody", "secret")).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    [Arguments("", "password")]
    [Arguments("Alice", "")]
    [Arguments(" ", "password")]
    public async Task EmptyCredentialsCannotCreateAnAccount(string name, string password)
    {
        var accounts = new UserAccounts();
        await Assert.That(() => accounts.Register(name, password)).Throws<ArgumentException>();
        await Assert.That(accounts.Users.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CharactersBelongToTheirAccountAndNamesMustBeDistinctWithinThatAccount()
    {
        var accounts = new UserAccounts();
        var alice = accounts.Register("Alice", "secret");
        var bob = accounts.Register("Bob", "secret");
        var hero = Hero();
        alice.AddCharacter(hero);
        bob.AddCharacter(Hero());
        await Assert.That(() => alice.AddCharacter(Hero())).ThrowsExactly<InvalidOperationException>();
        await Assert.That(bob.Characters[0] == hero).IsFalse();
        await Assert.That(() => bob.RenameCharacter(hero, "Stolen")).ThrowsExactly<InvalidOperationException>();
        await Assert.That(hero.Name).IsEqualTo("Adventurer");
    }

    [Test]
    public async Task ParserHandlesQuotedNamesAndRejectsUnclosedQuotes()
    {
        await Assert.That(CommandLine.Split("spell learn \"Magic Missile\"").SequenceEqual(new[] { "spell", "learn", "Magic Missile" })).IsTrue();
        await Assert.That(() => CommandLine.Split("cast \"Magic Missile")).ThrowsExactly<ArgumentException>();
    }
}
