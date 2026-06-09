// File: src/AGIQ.HexAssignmentConverter/Program.cs
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

return await Cli.RunAsync(args);

internal static class Cli
{
    public static Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return Task.FromResult(1);
        }

        try
        {
            var command = args[0].Trim().ToLowerInvariant();

            return command switch
            {
                "hex2cnfsolution" => Task.FromResult(RunHexToAssignments(args.Skip(1).ToArray())),
                "assignment2hex" => Task.FromResult(RunAssignmentsToHex(args.Skip(1).ToArray())),
                "--help" or "-h" or "help" => Task.FromResult(PrintHelp()),
                _ => Task.FromResult(UnknownCommand(command))
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
            return Task.FromResult(2);
        }
    }

    private static int RunHexToAssignments(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: hex2cnfsolution <hex-or-file> [--vars N]");

        string source = args[0];
        int? vars = null;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i].Equals("--vars", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException("Missing value after --vars");

                vars = int.Parse(args[++i], CultureInfo.InvariantCulture);
            }
            else
            {
                throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        string text = ReadTextOrValue(source);
        BigInteger value = ParseUnsignedHex(text);

        int bitCount = vars ?? Math.Max(1, GetBitLength(value));

        for (int i = 0; i < bitCount; i++)
        {
            bool bit = ((value >> i) & BigInteger.One) == BigInteger.One;
            Console.WriteLine($"x{i + 1} = {(bit ? 1 : 0)}");
        }

        return 0;
    }

    private static int RunAssignmentsToHex(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: assignment2hex <assignment-file>");

        string path = args[0];
        if (!File.Exists(path))
            throw new FileNotFoundException("Assignment file not found.", path);

        var assignments = LoadAssignments(path);
        if (assignments.Count == 0)
            throw new InvalidOperationException("No assignments found.");

        BigInteger value = BigInteger.Zero;

        foreach (var (index, bit) in assignments)
        {
            if (bit)
                value |= (BigInteger.One << (index - 1));
        }

        Console.WriteLine(ToUpperHex(value));
        return 0;
    }

    private static SortedDictionary<int, bool> LoadAssignments(string path)
    {
        var result = new SortedDictionary<int, bool>();
        var rx = new Regex(@"^\s*x(?<idx>\d+)\s*=\s*(?<val>[01])\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("#") || line.StartsWith("//") || line.StartsWith(";"))
                continue;

            var m = rx.Match(line);
            if (!m.Success)
                throw new FormatException($"Invalid assignment line: {raw}");

            int index = int.Parse(m.Groups["idx"].Value, CultureInfo.InvariantCulture);
            bool bit = m.Groups["val"].Value == "1";

            if (index < 1)
                throw new FormatException("Variable indices must start from x1.");

            result[index] = bit;
        }

        return result;
    }

    private static string ReadTextOrValue(string source)
    {
        if (File.Exists(source))
            return File.ReadAllText(source);

        return source;
    }

    private static BigInteger ParseUnsignedHex(string input)
    {
        string hex = NormalizeHex(input);

        if (hex.Length == 0)
            return BigInteger.Zero;

        if ((hex.Length % 2) != 0)
            hex = "0" + hex;

        byte[] bigEndian = Convert.FromHexString(hex);
        byte[] littleEndian = new byte[bigEndian.Length + 1]; // +1 to force positive BigInteger

        for (int i = 0; i < bigEndian.Length; i++)
            littleEndian[i] = bigEndian[bigEndian.Length - 1 - i];

        littleEndian[^1] = 0x00;
        return new BigInteger(littleEndian);
    }

    private static string NormalizeHex(string value)
    {
        var chars = value
            .Trim()
            .Replace("0x", "", StringComparison.OrdinalIgnoreCase)
            .Replace("_", "")
            .Replace(" ", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace("\t", "");

        foreach (char c in chars)
        {
            bool ok = (c >= '0' && c <= '9') ||
                      (c >= 'a' && c <= 'f') ||
                      (c >= 'A' && c <= 'F');

            if (!ok)
                throw new FormatException($"Invalid HEX character: '{c}'");
        }

        return chars;
    }

    private static int GetBitLength(BigInteger value)
    {
        if (value.Sign == 0)
            return 1;

        int bits = 0;
        BigInteger x = value;

        while (x > 0)
        {
            x >>= 1;
            bits++;
        }

        return bits;
    }

    private static string ToUpperHex(BigInteger value)
    {
        if (value.Sign < 0)
            throw new InvalidOperationException("Only non-negative values are supported.");

        return value.Sign == 0 ? "0" : value.ToString("X", CultureInfo.InvariantCulture);
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }

    private static int PrintHelp()
    {
        Console.WriteLine("""
AGIQ.HexAssignmentConverter

Commands:
  hex2cnfsolution <hex-or-file> [--vars N]
      Converts HEX into a CNF-style assignment:
      x1 = 1
      x2 = 0
      ...

  assignment2hex <assignment-file>
      Converts a text assignment file into HEX.

Examples:
  AGIQ.HexAssignmentConverter hex2cnfsolution 7A3F91C2AB --vars 40
  AGIQ.HexAssignmentConverter hex2cnfsolution result.hex --vars 64
  AGIQ.HexAssignmentConverter assignment2hex solution.txt

Notes:
  - x1 is the least significant bit (LSB).
  - If HEX contains leading zero bits that matter, pass --vars explicitly.
""");
        return 0;
    }
}
