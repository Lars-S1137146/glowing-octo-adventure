using System.Security.Cryptography;
using LongRest.Domain;

namespace LongRest.ConsoleApp;

public sealed class User
{
    private readonly byte[] salt;
    private readonly byte[] passwordHash;
    private readonly List<Character> characters = [];

    public string Name { get; }
    public IReadOnlyList<Character> Characters => characters.AsReadOnly();
    internal byte[] PasswordSalt => (byte[])salt.Clone();
    internal byte[] PasswordHash => (byte[])passwordHash.Clone();

    internal User(string name, string password)
    {
        Name = name;
        salt = RandomNumberGenerator.GetBytes(16);
        passwordHash = Hash(password);
    }

    internal User(string name, byte[] salt, byte[] passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 64 || salt.Length != 16 || passwordHash.Length != 32)
            throw new InvalidDataException("Invalid saved account credentials.");
        Name = name;
        this.salt = (byte[])salt.Clone();
        this.passwordHash = (byte[])passwordHash.Clone();
    }

    private byte[] Hash(string password) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

    internal bool MatchesPassword(string password) =>
        CryptographicOperations.FixedTimeEquals(passwordHash, Hash(password));

    public void AddCharacter(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (characters.Any(existing => existing.Id == character.Id ||
                existing.Name.Equals(character.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("You already have a character with that name.");
        characters.Add(character);
    }

    public Character FindCharacter(string name) => characters.FirstOrDefault(character =>
        character.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
        throw new InvalidOperationException("No character with that name belongs to this account.");

    public void RenameCharacter(Character character, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!characters.Contains(character)) throw new InvalidOperationException("This character does not belong to you.");
        if (characters.Any(other => other != character && other.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("You already have a character with that name.");
        character.Rename(name);
    }
}

public sealed class UserAccounts
{
    private readonly List<User> users;
    private readonly SqliteStorage? storage;
    public IReadOnlyList<User> Users => users.AsReadOnly();
    public string? DatabasePath => storage?.DatabasePath;

    public UserAccounts(SqliteStorage? storage = null)
    {
        this.storage = storage;
        users = storage?.LoadUsers() ?? [];
    }

    public void Save() => storage?.SaveUsers(users);

    public User Register(string name, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (name.Trim().Length > 64) throw new ArgumentException("Usernames can contain at most 64 characters.");
        if (password.Length > 1024) throw new ArgumentException("Password is too long.");
        name = name.Trim();
        if (users.Any(user => user.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("That username is already registered.");
        var user = new User(name, password);
        users.Add(user);
        return user;
    }

    public User Login(string name, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (password.Length > 1024) throw new ArgumentException("Password is too long.");
        var user = users.FirstOrDefault(user => user.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user is null || !user.MatchesPassword(password))
            throw new InvalidOperationException("Invalid username or password.");
        return user;
    }
}
