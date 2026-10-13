using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 2 &&
                args[0] == "--hold-game-ms" &&
                int.TryParse(
                    args[1],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int holdMilliseconds) &&
                holdMilliseconds is >= 1 and <= 10000)
            {
                Thread.Sleep(holdMilliseconds);
                return 0;
            }

            Dictionary<string, string> values = Parse(args);
            string operationId = values["--trial-operation-id"];
            string slotId = values["--trial-slot-id"];
            string version = values["--trial-version"];
            string signalsRoot = Path.GetFullPath(
                values["--trial-signal-directory"]);

            using Process current = Process.GetCurrentProcess();
            string executablePath = Path.GetFullPath(
                current.MainModule?.FileName ??
                throw new InvalidOperationException(
                    "Fixture executable path is unavailable."));
            var receipt = new
            {
                schemaVersion = 1,
                operationId,
                slotId,
                version,
                pid = current.Id,
                processStartTimeUtc = new DateTimeOffset(
                    current.StartTime.ToUniversalTime()),
                executablePath,
                healthyAtUtc = DateTimeOffset.UtcNow
            };

            string targetPath = Path.Combine(
                signalsRoot,
                operationId + ".healthy.json");
            string temporaryPath = Path.Combine(
                signalsRoot,
                "." + operationId + "." +
                Guid.NewGuid().ToString("N") + ".tmp");
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(receipt);
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       4096,
                       FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, targetPath);

            Thread.Sleep(TimeSpan.FromSeconds(60));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        string[] switches =
        [
            "--trial-operation-id",
            "--trial-slot-id",
            "--trial-version",
            "--trial-signal-directory"
        ];
        if (args.Length != switches.Length * 2)
            throw new InvalidDataException(
                "Fixture requires exactly four switch/value pairs.");

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < args.Length; index += 2)
        {
            if (args[index] != switches[index / 2] ||
                string.IsNullOrWhiteSpace(args[index + 1]) ||
                args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Fixture arguments are invalid.");
            }
            values.Add(args[index], args[index + 1]);
        }

        if (!IsHex(values["--trial-operation-id"], 32) ||
            !values["--trial-slot-id"].StartsWith(
                "slot-", StringComparison.Ordinal) ||
            values["--trial-slot-id"].Length != 37 ||
            !IsHex(values["--trial-slot-id"][5..], 32) ||
            !Version.TryParse(values["--trial-version"], out Version? version) ||
            version.Build < 0)
        {
            throw new InvalidDataException(
                "Fixture trial identity is invalid.");
        }
        return values;
    }

    private static bool IsHex(string value, int length)
    {
        if (value.Length != length)
            return false;
        foreach (char character in value)
        {
            if (!((character >= '0' && character <= '9') ||
                  (character >= 'a' && character <= 'f') ||
                  (character >= 'A' && character <= 'F')))
                return false;
        }
        return true;
    }
}
