using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace LEADKhongPhanh.Update
{
    public sealed class JsonUpdateProvider : IUpdateProvider
    {
        private static readonly UTF8Encoding StrictUtf8 =
            new UTF8Encoding(false, true);

        private readonly UpdateConfig config;

        public JsonUpdateProvider(UpdateConfig config)
        {
            this.config = config ??
                throw new ArgumentNullException(nameof(config));
        }

        public async Task<UpdateManifestResult> GetLatestManifestAsync(
            CancellationToken cancellationToken)
        {
            if (!config.TryValidate(out UpdateValidationError configError))
                return UpdateManifestResult.Failure(configError);

            if (cancellationToken.IsCancellationRequested)
            {
                return UpdateManifestResult.Failure(
                    UpdateValidationError.Cancelled);
            }

            if (!UpdateManifest.TryParseHttpsUrl(
                    config.ManifestUrl,
                    out Uri manifestUri,
                    out _))
            {
                return UpdateManifestResult.Failure(
                    UpdateValidationError.InvalidConfiguration);
            }

            using (var request = new UnityWebRequest(
                       manifestUri,
                       UnityWebRequest.kHttpVerbGET))
            {
                var handler = new BoundedManifestDownloadHandler(
                    config.MaxManifestBytes);
                request.downloadHandler = handler;
                request.timeout = config.RequestTimeoutSeconds;
                request.redirectLimit = 0;

                using (cancellationToken.Register(request.Abort))
                {
                    UnityWebRequestAsyncOperation operation;

                    try
                    {
                        operation = request.SendWebRequest();
                        await AwaitCompletionAsync(operation);
                    }
                    catch (Exception)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return UpdateManifestResult.Failure(
                                UpdateValidationError.Cancelled);
                        }

                        return UpdateManifestResult.Failure(
                            UpdateValidationError.RequestFailed);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return UpdateManifestResult.Failure(
                        UpdateValidationError.Cancelled);
                }

                if (handler.LimitExceeded)
                {
                    return UpdateManifestResult.Failure(
                        UpdateValidationError.ManifestTooLarge);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    UpdateValidationError requestError =
                        request.result ==
                        UnityWebRequest.Result.ConnectionError
                            ? UpdateValidationError.NetworkUnavailable
                            : UpdateValidationError.RequestFailed;

                    return UpdateManifestResult.Failure(requestError);
                }

                if (!handler.TryGetText(out string json))
                {
                    return UpdateManifestResult.Failure(
                        UpdateValidationError.MalformedJson);
                }

                if (!TryParseManifestJson(
                        json,
                        config.MaxManifestBytes,
                        config.MaxPackageBytes,
                        config.MaxReleaseNotesBytes,
                        out UpdateManifest manifest,
                        out UpdateValidationError parseError))
                {
                    return UpdateManifestResult.Failure(parseError);
                }

                return UpdateManifestResult.Success(manifest);
            }
        }

        public static bool TryParseManifestJson(
            string json,
            int maximumManifestBytes,
            long maximumPackageBytes,
            int maximumReleaseNotesBytes,
            out UpdateManifest manifest,
            out UpdateValidationError error)
        {
            manifest = null;
            error = UpdateValidationError.None;

            if (maximumManifestBytes < 1 ||
                maximumManifestBytes >
                    UpdateManifest.ProtocolMaximumManifestBytes ||
                maximumPackageBytes <= 0 ||
                maximumReleaseNotesBytes < 0 ||
                maximumReleaseNotesBytes >
                    UpdateManifest.ProtocolMaximumReleaseNotesBytes)
            {
                error = UpdateValidationError.InvalidConfiguration;
                return false;
            }

            if (json == null)
            {
                error = UpdateValidationError.MalformedJson;
                return false;
            }

            int jsonByteCount;
            try
            {
                jsonByteCount = StrictUtf8.GetByteCount(json);
            }
            catch (EncoderFallbackException)
            {
                error = UpdateValidationError.MalformedJson;
                return false;
            }

            if (jsonByteCount > maximumManifestBytes)
            {
                error = UpdateValidationError.ManifestTooLarge;
                return false;
            }

            var reader = new ManifestJsonReader(json);
            if (!reader.TryRead(
                    out Dictionary<string, string> fields,
                    out error))
            {
                return false;
            }

            if (!TryGetRequiredField(fields, "version", out string version) ||
                !TryGetRequiredField(
                    fields,
                    "minimumSupportedVersion",
                    out string minimumVersion) ||
                !TryGetRequiredField(
                    fields,
                    "downloadUrl",
                    out string downloadUrl) ||
                !TryGetRequiredField(fields, "sha256", out string sha256) ||
                !TryGetRequiredField(fields, "fileSize", out string fileSizeText))
            {
                error = UpdateValidationError.MissingField;
                return false;
            }

            if (!long.TryParse(
                    fileSizeText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long fileSize))
            {
                error = UpdateValidationError.InvalidFileSize;
                return false;
            }

            fields.TryGetValue("releaseNotes", out string releaseNotes);

            return UpdateManifest.TryCreate(
                version,
                minimumVersion,
                downloadUrl,
                sha256,
                fileSize,
                releaseNotes,
                maximumPackageBytes,
                maximumReleaseNotesBytes,
                out manifest,
                out error);
        }

        private static bool TryGetRequiredField(
            Dictionary<string, string> fields,
            string name,
            out string value)
        {
            return fields.TryGetValue(name, out value);
        }

        private static Task AwaitCompletionAsync(
            UnityWebRequestAsyncOperation operation)
        {
            if (operation.isDone)
                return Task.CompletedTask;

            var completion = new TaskCompletionSource<bool>();
            Action<UnityEngine.AsyncOperation> onCompleted = null;
            onCompleted = _ =>
            {
                operation.completed -= onCompleted;
                completion.TrySetResult(true);
            };

            operation.completed += onCompleted;
            if (operation.isDone)
            {
                operation.completed -= onCompleted;
                completion.TrySetResult(true);
            }

            return completion.Task;
        }

        private sealed class BoundedManifestDownloadHandler :
            DownloadHandlerScript
        {
            private readonly byte[] buffer;
            private int length;

            public bool LimitExceeded { get; private set; }

            public BoundedManifestDownloadHandler(int maximumBytes)
                : base(new byte[4096])
            {
                buffer = new byte[maximumBytes];
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength < 0)
                    return false;

                if (dataLength > buffer.Length - length)
                {
                    LimitExceeded = true;
                    return false;
                }

                Buffer.BlockCopy(data, 0, buffer, length, dataLength);
                length += dataLength;
                return true;
            }

            public bool TryGetText(out string text)
            {
                try
                {
                    text = StrictUtf8.GetString(buffer, 0, length);
                    return true;
                }
                catch (DecoderFallbackException)
                {
                    text = null;
                    return false;
                }
            }
        }

        private sealed class ManifestJsonReader
        {
            private readonly string json;
            private int position;

            public ManifestJsonReader(string json)
            {
                this.json = json;
            }

            public bool TryRead(
                out Dictionary<string, string> fields,
                out UpdateValidationError error)
            {
                fields = new Dictionary<string, string>(
                    StringComparer.Ordinal);
                error = UpdateValidationError.None;

                SkipWhitespace();
                if (!Consume('{'))
                    return Fail(UpdateValidationError.MalformedJson, out error);

                SkipWhitespace();
                if (Consume('}'))
                    return Finish(fields, out error);

                while (position < json.Length)
                {
                    if (!TryReadString(out string propertyName))
                        return Fail(
                            UpdateValidationError.MalformedJson,
                            out error);

                    if (!IsKnownField(propertyName))
                        return Fail(
                            UpdateValidationError.UnknownField,
                            out error);

                    if (fields.ContainsKey(propertyName))
                        return Fail(
                            UpdateValidationError.MalformedJson,
                            out error);

                    SkipWhitespace();
                    if (!Consume(':'))
                        return Fail(
                            UpdateValidationError.MalformedJson,
                            out error);

                    SkipWhitespace();

                    string value;
                    if (propertyName == "fileSize")
                    {
                        if (!TryReadInteger(out value))
                        {
                            return Fail(
                                UpdateValidationError.InvalidFieldType,
                                out error);
                        }
                    }
                    else if (!TryReadString(out value))
                    {
                        return Fail(
                            UpdateValidationError.InvalidFieldType,
                            out error);
                    }

                    fields.Add(propertyName, value);
                    SkipWhitespace();

                    if (Consume('}'))
                        return Finish(fields, out error);

                    if (!Consume(','))
                        return Fail(
                            UpdateValidationError.MalformedJson,
                            out error);

                    SkipWhitespace();
                }

                return Fail(
                    UpdateValidationError.MalformedJson,
                    out error);
            }

            private bool Finish(
                Dictionary<string, string> fields,
                out UpdateValidationError error)
            {
                SkipWhitespace();
                if (position != json.Length)
                    return Fail(
                        UpdateValidationError.MalformedJson,
                        out error);

                error = UpdateValidationError.None;
                return true;
            }

            private bool TryReadString(out string value)
            {
                value = null;
                if (!Consume('"'))
                    return false;

                var builder = new StringBuilder();
                while (position < json.Length)
                {
                    char character = json[position++];

                    if (character == '"')
                    {
                        value = builder.ToString();
                        return true;
                    }

                    if (character < 0x20)
                        return false;

                    if (character == '\\')
                    {
                        if (!TryReadEscape(builder))
                            return false;
                    }
                    else
                    {
                        if (char.IsHighSurrogate(character))
                        {
                            if (position >= json.Length ||
                                !char.IsLowSurrogate(json[position]))
                            {
                                return false;
                            }

                            builder.Append(character);
                            builder.Append(json[position++]);
                        }
                        else if (char.IsLowSurrogate(character))
                        {
                            return false;
                        }
                        else
                        {
                            builder.Append(character);
                        }
                    }
                }

                return false;
            }

            private bool TryReadEscape(StringBuilder builder)
            {
                if (position >= json.Length)
                    return false;

                char escape = json[position++];
                switch (escape)
                {
                    case '"':
                    case '\\':
                    case '/':
                        builder.Append(escape);
                        return true;
                    case 'b':
                        builder.Append('\b');
                        return true;
                    case 'f':
                        builder.Append('\f');
                        return true;
                    case 'n':
                        builder.Append('\n');
                        return true;
                    case 'r':
                        builder.Append('\r');
                        return true;
                    case 't':
                        builder.Append('\t');
                        return true;
                    case 'u':
                        return TryReadUnicodeEscape(builder);
                    default:
                        return false;
                }
            }

            private bool TryReadUnicodeEscape(StringBuilder builder)
            {
                if (!TryReadHexCodeUnit(out char codeUnit))
                    return false;

                if (char.IsHighSurrogate(codeUnit))
                {
                    if (position + 2 > json.Length ||
                        json[position] != '\\' ||
                        json[position + 1] != 'u')
                    {
                        return false;
                    }

                    position += 2;
                    if (!TryReadHexCodeUnit(out char lowSurrogate) ||
                        !char.IsLowSurrogate(lowSurrogate))
                    {
                        return false;
                    }

                    builder.Append(codeUnit);
                    builder.Append(lowSurrogate);
                    return true;
                }

                if (char.IsLowSurrogate(codeUnit))
                    return false;

                builder.Append(codeUnit);
                return true;
            }

            private bool TryReadHexCodeUnit(out char value)
            {
                value = '\0';
                if (position + 4 > json.Length)
                    return false;

                int codePoint = 0;
                for (int i = 0; i < 4; i++)
                {
                    int digit = HexValue(json[position++]);
                    if (digit < 0)
                        return false;

                    codePoint = (codePoint << 4) | digit;
                }

                value = (char)codePoint;
                return true;
            }

            private bool TryReadInteger(out string value)
            {
                int start = position;
                value = null;

                if (position < json.Length && json[position] == '-')
                    position++;

                if (position >= json.Length)
                    return false;

                if (json[position] == '0')
                {
                    position++;
                    if (position < json.Length &&
                        IsAsciiDigit(json[position]))
                    {
                        return false;
                    }
                }
                else
                {
                    if (json[position] < '1' || json[position] > '9')
                        return false;

                    while (position < json.Length &&
                           IsAsciiDigit(json[position]))
                    {
                        position++;
                    }
                }

                if (position < json.Length &&
                    (json[position] == '.' ||
                     json[position] == 'e' ||
                     json[position] == 'E'))
                {
                    return false;
                }

                value = json.Substring(start, position - start);
                return true;
            }

            private static bool IsKnownField(string name)
            {
                return name == "version" ||
                       name == "minimumSupportedVersion" ||
                       name == "downloadUrl" ||
                       name == "sha256" ||
                       name == "fileSize" ||
                       name == "releaseNotes";
            }

            private static bool IsAsciiDigit(char value)
            {
                return value >= '0' && value <= '9';
            }

            private static int HexValue(char value)
            {
                if (value >= '0' && value <= '9')
                    return value - '0';
                if (value >= 'a' && value <= 'f')
                    return value - 'a' + 10;
                if (value >= 'A' && value <= 'F')
                    return value - 'A' + 10;
                return -1;
            }

            private void SkipWhitespace()
            {
                while (position < json.Length)
                {
                    char character = json[position];
                    if (character != ' ' &&
                        character != '\t' &&
                        character != '\r' &&
                        character != '\n')
                    {
                        return;
                    }

                    position++;
                }
            }

            private bool Consume(char expected)
            {
                if (position >= json.Length || json[position] != expected)
                    return false;

                position++;
                return true;
            }

            private static bool Fail(
                UpdateValidationError failure,
                out UpdateValidationError error)
            {
                error = failure;
                return false;
            }
        }
    }
}
