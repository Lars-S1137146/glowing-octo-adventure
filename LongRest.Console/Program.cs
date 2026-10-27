using LongRest.ConsoleApp;
using Microsoft.Data.Sqlite;

if (args.Length != 0 && (args.Length != 2 || args[0] != "--database"))
{
    Console.Error.WriteLine("Usage: LongRest.Console [--database <path>]");
    Environment.ExitCode = 2;
    return;
}

try
{
    var storage = new SqliteStorage(args.Length == 2 ? args[1] : SqliteStorage.DefaultPath);
    var accounts = new UserAccounts(storage);
    new ConsoleApplication(Console.In, Console.Out, accounts, ReadPassword).Run();
}
catch (Exception exception) when (exception is SqliteException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine("Storage error: " + exception.Message);
    Console.Error.WriteLine("The application has stopped. Changes from the last command may not have been saved.");
    Environment.ExitCode = 1;
}

static string? ReadPassword()
{
    if (Console.IsInputRedirected) return Console.ReadLine();
    var password = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return password.ToString(); }
        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0) password.Length--;
        }
        else if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
    }
}
