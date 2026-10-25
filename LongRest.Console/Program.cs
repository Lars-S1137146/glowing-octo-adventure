using LongRest.ConsoleApp;

new ConsoleApplication(Console.In, Console.Out, readPassword: ReadPassword).Run();

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
