namespace ModernWinManager.Services;

internal enum MessageKind { Info, Success, Warning, Error }

internal static class MenuService
{
    private static readonly Dictionary<MessageKind, ConsoleColor> Colors = new()
    {
        [MessageKind.Info]    = ConsoleColor.Gray,
        [MessageKind.Success] = ConsoleColor.Green,
        [MessageKind.Warning] = ConsoleColor.DarkYellow,
        [MessageKind.Error]   = ConsoleColor.Red,
    };

    private static string? _pendingMessage;
    private static MessageKind _pendingKind;
    private static string _lastTitle = "";
    private static string _lastScreenInfo = "";

    public static void SetPendingMessage(string text, MessageKind kind = MessageKind.Info)
    {
        _pendingMessage = text;
        _pendingKind = kind;
    }

    public static void ShowHeader(string title, string screenInfo)
    {
        _lastTitle = title;
        _lastScreenInfo = screenInfo;
        PrintHeader();
    }

    private static void PrintHeader()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== {_lastTitle} ===");
        Console.ResetColor();
        Console.WriteLine($"Skärmkonfig: {_lastScreenInfo}");

        if (_pendingMessage != null)
        {
            Console.ForegroundColor = Colors[_pendingKind];
            Console.WriteLine(_pendingMessage);
            Console.ResetColor();
            _pendingMessage = null;
        }

        Console.WriteLine();
    }

    private const int PageSize = 20;

    private static string Truncate(string text, int maxWidth)
    {
        if (text.Length <= maxWidth) return text;
        return text[..(maxWidth - 1)] + "…";
    }

    public static int PickOption(string prompt, List<string> options, out ConsoleKeyInfo? specialKey)
    {
        specialKey = null;
        int page = 0;
        int totalPages = (options.Count + PageSize - 1) / PageSize;

        while (true)
        {
            int start = page * PageSize;
            int end = Math.Min(start + PageSize, options.Count);

            // Reprint from current cursor position (caller already printed header)
            // Clear previous page content by reprinting
            Console.WriteLine();
            for (int i = start; i < end; i++)
            {
                var prefix = $"  {i - start + 1}. ";
                var text = Truncate(options[i], Console.WindowWidth - prefix.Length - 1);
                Console.WriteLine($"{prefix}{text}");
            }

            Console.WriteLine();
            if (totalPages > 1)
                Console.WriteLine($"  Sida {page + 1}/{totalPages}  (↑↓ = bläddra)");
            Console.Write($"{prompt} ");

            var key = Console.ReadKey(intercept: true);

            if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.E)
            {
                specialKey = key;
                Console.WriteLine();
                return -1;
            }

            if (key.Key == ConsoleKey.Q && !key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                specialKey = key;
                Console.WriteLine();
                return -1;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                specialKey = key;
                Console.WriteLine();
                return -1;
            }

            if (key.Key == ConsoleKey.DownArrow)
            {
                if (page < totalPages - 1) page++;
                PrintHeader();
                continue;
            }

            if (key.Key == ConsoleKey.UpArrow)
            {
                if (page > 0) page--;
                PrintHeader();
                continue;
            }

            if (char.IsDigit(key.KeyChar))
            {
                int digit = key.KeyChar - '0';
                int pageCount = end - start;

                // 1 and 2 may be the start of a two-digit number (10-20)
                if ((digit == 1 || digit == 2) && pageCount > 9)
                {
                    Console.Write(key.KeyChar);
                    var next = Console.ReadKey(intercept: true);

                    if (next.Key == ConsoleKey.Enter)
                    {
                        // Confirmed as single digit
                        Console.WriteLine();
                        int globalIndex = start + digit - 1;
                        if (digit >= 1 && digit <= pageCount) return globalIndex;
                    }
                    else if (char.IsDigit(next.KeyChar))
                    {
                        int combined = digit * 10 + (next.KeyChar - '0');
                        Console.WriteLine(next.KeyChar);
                        int globalIndex = start + combined - 1;
                        if (combined >= 1 && combined <= pageCount) return globalIndex;
                    }
                    else if (next.Modifiers.HasFlag(ConsoleModifiers.Control) && next.Key == ConsoleKey.E)
                    {
                        specialKey = next;
                        Console.WriteLine();
                        return -1;
                    }
                    else if (next.Key == ConsoleKey.Escape)
                    {
                        specialKey = next;
                        Console.WriteLine();
                        return -1;
                    }
                }
                else
                {
                    Console.WriteLine(key.KeyChar);
                    int globalIndex = start + digit - 1;
                    if (digit >= 1 && digit <= pageCount) return globalIndex;
                }
            }
        }
    }


}
