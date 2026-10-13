using System;
using System.Text;

namespace LEADKhongPhanh.Update
{
    public enum UpdateValidationError
    {
        None,
        InvalidConfiguration,
        ManifestTooLarge,
        MalformedJson,
        MissingField,
        InvalidFieldType,
        UnknownField,
        InvalidVersion,
        InvalidMinimumVersion,
        MinimumVersionGreaterThanLatest,
        InvalidUrl,
        NonHttpsUrl,
        UrlContainsCredentials,
        InvalidSha256,
        InvalidFileSize,
        FileSizeTooLarge,
        ReleaseNotesTooLarge,
        NetworkUnavailable,
        RequestFailed,
        Cancelled
    }

    public sealed class UpdateManifest
    {
        private static readonly UTF8Encoding StrictUtf8 =
            new UTF8Encoding(false, true);

        public const int ProtocolMaximumManifestBytes = 64 * 1024;
        public const int ProtocolMaximumReleaseNotesBytes = 8 * 1024;

        public UpdateVersion Version { get; }
        public UpdateVersion MinimumSupportedVersion { get; }
        public Uri DownloadUrl { get; }
        public string Sha256 { get; }
        public long FileSize { get; }
        public string ReleaseNotes { get; }

        private UpdateManifest(
            UpdateVersion version,
            UpdateVersion minimumSupportedVersion,
            Uri downloadUrl,
            string sha256,
            long fileSize,
            string releaseNotes)
        {
            Version = version;
            MinimumSupportedVersion = minimumSupportedVersion;
            DownloadUrl = downloadUrl;
            Sha256 = sha256;
            FileSize = fileSize;
            ReleaseNotes = releaseNotes;
        }

        public static bool TryCreate(
            string version,
            string minimumSupportedVersion,
            string downloadUrl,
            string sha256,
            long fileSize,
            string releaseNotes,
            long maximumPackageBytes,
            int maximumReleaseNotesBytes,
            out UpdateManifest manifest,
            out UpdateValidationError error)
        {
            manifest = null;
            error = UpdateValidationError.None;

            if (maximumPackageBytes <= 0 ||
                maximumReleaseNotesBytes < 0 ||
                maximumReleaseNotesBytes >
                    ProtocolMaximumReleaseNotesBytes)
            {
                error = UpdateValidationError.InvalidConfiguration;
                return false;
            }

            if (!UpdateVersion.TryParse(version, out UpdateVersion parsedVersion))
            {
                error = UpdateValidationError.InvalidVersion;
                return false;
            }

            if (!UpdateVersion.TryParse(
                    minimumSupportedVersion,
                    out UpdateVersion parsedMinimumVersion))
            {
                error = UpdateValidationError.InvalidMinimumVersion;
                return false;
            }

            if (parsedMinimumVersion > parsedVersion)
            {
                error =
                    UpdateValidationError.MinimumVersionGreaterThanLatest;
                return false;
            }

            if (!TryParseHttpsUrl(
                    downloadUrl,
                    out Uri parsedDownloadUrl,
                    out error))
            {
                return false;
            }

            if (!IsValidSha256(sha256))
            {
                error = UpdateValidationError.InvalidSha256;
                return false;
            }

            if (fileSize <= 0)
            {
                error = UpdateValidationError.InvalidFileSize;
                return false;
            }

            if (fileSize > maximumPackageBytes)
            {
                error = UpdateValidationError.FileSizeTooLarge;
                return false;
            }

            string notes = releaseNotes ?? string.Empty;
            int notesByteLength;

            try
            {
                notesByteLength = StrictUtf8.GetByteCount(notes);
            }
            catch (EncoderFallbackException)
            {
                error = UpdateValidationError.InvalidFieldType;
                return false;
            }

            if (notesByteLength > maximumReleaseNotesBytes)
            {
                error = UpdateValidationError.ReleaseNotesTooLarge;
                return false;
            }

            manifest = new UpdateManifest(
                parsedVersion,
                parsedMinimumVersion,
                parsedDownloadUrl,
                sha256.ToLowerInvariant(),
                fileSize,
                notes);
            return true;
        }

        internal static bool TryParseHttpsUrl(
            string value,
            out Uri uri,
            out UpdateValidationError error)
        {
            uri = null;
            error = UpdateValidationError.None;

            if (string.IsNullOrEmpty(value) ||
                !Uri.TryCreate(value, UriKind.Absolute, out Uri parsedUri))
            {
                error = UpdateValidationError.InvalidUrl;
                return false;
            }

            if (!string.Equals(
                    parsedUri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase))
            {
                error = UpdateValidationError.NonHttpsUrl;
                return false;
            }

            if (!string.Equals(
                    value,
                    value.Trim(),
                    StringComparison.Ordinal) ||
                ContainsUnsafeUrlCharacter(value))
            {
                error = UpdateValidationError.InvalidUrl;
                return false;
            }

            if (string.IsNullOrEmpty(parsedUri.Host))
            {
                error = UpdateValidationError.InvalidUrl;
                return false;
            }

            if (!string.IsNullOrEmpty(parsedUri.UserInfo))
            {
                error = UpdateValidationError.UrlContainsCredentials;
                return false;
            }

            uri = parsedUri;
            return true;
        }

        private static bool ContainsUnsafeUrlCharacter(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character <= 0x20 ||
                    character == 0x7f ||
                    character == '\\')
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidSha256(string value)
        {
            if (value == null || value.Length != 64)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                bool isDigit = character >= '0' && character <= '9';
                bool isLowerHex = character >= 'a' && character <= 'f';
                bool isUpperHex = character >= 'A' && character <= 'F';

                if (!isDigit && !isLowerHex && !isUpperHex)
                    return false;
            }

            return true;
        }
    }

    public readonly struct UpdateManifestResult
    {
        public UpdateManifest Manifest { get; }
        public UpdateValidationError Error { get; }
        public bool IsSuccess =>
            Manifest != null && Error == UpdateValidationError.None;

        private UpdateManifestResult(
            UpdateManifest manifest,
            UpdateValidationError error)
        {
            Manifest = manifest;
            Error = error;
        }

        public static UpdateManifestResult Success(UpdateManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));

            return new UpdateManifestResult(
                manifest,
                UpdateValidationError.None);
        }

        public static UpdateManifestResult Failure(
            UpdateValidationError error)
        {
            if (error == UpdateValidationError.None)
                throw new ArgumentException(
                    "Failure result must contain an error.",
                    nameof(error));

            return new UpdateManifestResult(null, error);
        }
    }
}
