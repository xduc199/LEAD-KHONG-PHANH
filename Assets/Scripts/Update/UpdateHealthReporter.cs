using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LEADKhongPhanh.Update
{
    [DisallowMultipleComponent]
    public sealed class UpdateHealthReporter : MonoBehaviour
    {
        private const string SignalsSwitch = "--trial-signal-directory";
        private const string PublisherDirectory = "LEAD-KHONG-PHANH";
        private static bool reported;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallReporter()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var reporterObject = new GameObject(
                nameof(UpdateHealthReporter));
            DontDestroyOnLoad(reporterObject);
            reporterObject.AddComponent<UpdateHealthReporter>();
#endif
        }

        private void Start()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (reported)
                return;
            reported = true;

            try
            {
                if (!TryReadTrialArguments(
                        Environment.GetCommandLineArgs(),
                        out Dictionary<string, string> values))
                {
                    return;
                }

                WriteHealthReceipt(values);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[UpdateHealthReporter] Trial health receipt failed: " +
                    exception.Message,
                    this);
            }
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static bool TryReadTrialArguments(
            string[] arguments,
            out Dictionary<string, string> values)
        {
            values = null;
            bool hasTrialArgument = false;
            for (int index = 1; index < arguments.Length; index++)
            {
                if (arguments[index].StartsWith(
                        "--trial-",
                        StringComparison.Ordinal))
                {
                    hasTrialArgument = true;
                    break;
                }
            }
            if (!hasTrialArgument)
                return false;

            if (arguments.Length != 9)
                throw new InvalidDataException(
                    "Trial launch must contain exactly four switch/value pairs.");

            var parsed = new Dictionary<string, string>(
                StringComparer.Ordinal);
            string[] allowed =
            {
                "--trial-operation-id",
                "--trial-slot-id",
                "--trial-version",
                SignalsSwitch
            };
            for (int index = 1; index < arguments.Length; index += 2)
            {
                string key = arguments[index];
                string value = arguments[index + 1];
                if (Array.IndexOf(allowed, key) < 0 ||
                    string.IsNullOrWhiteSpace(value) ||
                    value.StartsWith("--", StringComparison.Ordinal) ||
                    !parsed.TryAdd(key, value))
                {
                    throw new InvalidDataException(
                        "Trial launch arguments are malformed.");
                }
            }
            if (parsed.Count != allowed.Length)
                throw new InvalidDataException(
                    "A trial launch argument is missing.");

            values = parsed;
            return true;
        }

        private static void WriteHealthReceipt(
            Dictionary<string, string> values)
        {
            string operationId = values["--trial-operation-id"];
            string slotId = values["--trial-slot-id"];
            string version = values["--trial-version"];
            string suppliedSignals = values[SignalsSwitch];
            if (!IsHex(operationId, 32) ||
                !IsSlotId(slotId) ||
                !UpdateVersion.TryParse(version, out _) ||
                !version.Equals(Application.version, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Trial identity does not match this application build.");
            }

            string dataPath = Path.GetFullPath(Application.dataPath);
            if (!Path.GetFileName(dataPath).Equals(
                    "LEADKhongPhanh_Data",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Unity data directory does not match the trusted layout.");
            }

            DirectoryInfo slotDirectory = Directory.GetParent(dataPath);
            DirectoryInfo slotsDirectory = slotDirectory?.Parent;
            DirectoryInfo installationDirectory = slotsDirectory?.Parent;
            if (slotDirectory == null ||
                slotsDirectory == null ||
                installationDirectory == null ||
                !slotDirectory.Name.Equals(
                    slotId,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Trial slot directory does not match the launch identity.");
            }

            string installationRoot = installationDirectory.FullName;
            string installIdPath = Path.Combine(
                installationRoot, "install.id");
            string installId = File.ReadAllText(installIdPath).Trim();
            if (!IsHex(installId, 32))
                throw new InvalidDataException(
                    "Installation identity is malformed.");

            string localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            string signalsRoot = Path.GetFullPath(Path.Combine(
                localAppData,
                PublisherDirectory,
                installId.ToLowerInvariant(),
                "Updater",
                "Signals"));
            if (!Path.GetFullPath(suppliedSignals).Equals(
                    signalsRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Trial signal directory is not the trusted updater Signals directory.");
            }
            EnsureNoReparsePoints(localAppData, signalsRoot);
            if (!Directory.Exists(signalsRoot))
                throw new DirectoryNotFoundException(
                    "Trusted updater Signals directory does not exist.");

            using Process process = Process.GetCurrentProcess();
            string executablePath = Path.GetFullPath(
                process.MainModule?.FileName ??
                throw new InvalidOperationException(
                    "Current executable path could not be resolved."));
            string expectedExecutable = Path.GetFullPath(Path.Combine(
                slotDirectory.FullName,
                "LEADKhongPhanh.exe"));
            if (!executablePath.Equals(
                    expectedExecutable,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Running executable is not inside the trial slot.");
            }

            var receipt = new HealthSignal
            {
                schemaVersion = 1,
                operationId = operationId,
                slotId = slotId,
                version = version,
                pid = process.Id,
                processStartTimeUtc = new DateTimeOffset(
                    process.StartTime.ToUniversalTime()).ToString("O"),
                executablePath = executablePath,
                healthyAtUtc = DateTimeOffset.UtcNow.ToString("O")
            };

            string receiptPath = Path.Combine(
                signalsRoot,
                operationId + ".healthy.json");
            if (File.Exists(receiptPath) || Directory.Exists(receiptPath))
                throw new IOException(
                    "A trial receipt already exists and will not be overwritten.");

            string temporaryPath = Path.Combine(
                signalsRoot,
                "." + operationId + "." +
                Guid.NewGuid().ToString("N") + ".tmp");
            byte[] contents = new UTF8Encoding(false, true).GetBytes(
                JsonUtility.ToJson(receipt));
            try
            {
                using (var stream = new FileStream(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           4096,
                           FileOptions.WriteThrough))
                {
                    stream.Write(contents, 0, contents.Length);
                    stream.Flush(true);
                }
                File.Move(temporaryPath, receiptPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static void EnsureNoReparsePoints(
            string boundaryRoot,
            string candidatePath)
        {
            string root = Path.GetFullPath(boundaryRoot);
            string candidate = Path.GetFullPath(candidatePath);
            bool rootHasTrailingSeparator =
                root.EndsWith(
                    Path.DirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal) ||
                root.EndsWith(
                    Path.AltDirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal);
            string prefix = rootHasTrailingSeparator
                ? root
                : root + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Trial signal path escapes the trusted root.");
            }

            string current = root;
            if ((File.GetAttributes(current) &
                 FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException(
                    "Trusted signal root is a reparse point.");
            }
            string relative = Path.GetRelativePath(root, candidate);
            foreach (string segment in relative.Split(
                         Path.DirectorySeparatorChar,
                         Path.AltDirectorySeparatorChar))
            {
                current = Path.Combine(current, segment);
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) &
                     FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidDataException(
                        "Trial signal path contains a reparse point.");
                }
            }
        }

        private static bool IsSlotId(string value) =>
            value != null &&
            value.Length == 37 &&
            value.StartsWith("slot-", StringComparison.Ordinal) &&
            IsHex(value.Substring(5), 32);

        private static bool IsHex(string value, int length)
        {
            if (value == null || value.Length != length)
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f') ||
                      (character >= 'A' && character <= 'F')))
                    return false;
            }
            return true;
        }

        [Serializable]
        private sealed class HealthSignal
        {
            public int schemaVersion;
            public string operationId;
            public string slotId;
            public string version;
            public int pid;
            public string processStartTimeUtc;
            public string executablePath;
            public string healthyAtUtc;
        }
#endif
    }
}
