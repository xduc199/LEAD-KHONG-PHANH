using System;
using UnityEngine;

namespace LEADKhongPhanh.Update
{
    [CreateAssetMenu(
        fileName = "UpdateConfig",
        menuName = "LEAD KHÔNG PHANH/Update/Update Config")]
    public sealed class UpdateConfig : ScriptableObject
    {
        [Header("Manifest")]
        [SerializeField] private string manifestUrl = string.Empty;
        [SerializeField, Min(1)] private int requestTimeoutSeconds = 10;
        [SerializeField, Min(1)] private int downloadTimeoutSeconds = 1800;
        [SerializeField, Min(1)] private int maxManifestBytes =
            UpdateManifest.ProtocolMaximumManifestBytes;
        [SerializeField, Min(1)] private long maxPackageBytes =
            4L * 1024L * 1024L * 1024L;
        [SerializeField, Min(0)] private int maxReleaseNotesBytes =
            UpdateManifest.ProtocolMaximumReleaseNotesBytes;

        [Header("Archive Limits")]
        [SerializeField, Min(1)] private int maxArchiveEntries = 50000;
        [SerializeField, Min(1)] private long maxExpandedBytes =
            8L * 1024L * 1024L * 1024L;
        [SerializeField, Min(1f)] private float maxCompressionRatio = 200f;

        public string ManifestUrl => manifestUrl;
        public int RequestTimeoutSeconds => requestTimeoutSeconds;
        public int DownloadTimeoutSeconds => downloadTimeoutSeconds;
        public int MaxManifestBytes => maxManifestBytes;
        public long MaxPackageBytes => maxPackageBytes;
        public int MaxReleaseNotesBytes => maxReleaseNotesBytes;
        public int MaxArchiveEntries => maxArchiveEntries;
        public long MaxExpandedBytes => maxExpandedBytes;
        public float MaxCompressionRatio => maxCompressionRatio;

        public bool TryValidate(out UpdateValidationError error)
        {
            if (!UpdateManifest.TryParseHttpsUrl(
                    manifestUrl,
                    out _,
                    out error))
            {
                return false;
            }

            if (requestTimeoutSeconds < 1 || requestTimeoutSeconds > 120 ||
                downloadTimeoutSeconds < 1 ||
                downloadTimeoutSeconds > 86400 ||
                maxManifestBytes < 1 ||
                maxManifestBytes >
                    UpdateManifest.ProtocolMaximumManifestBytes ||
                maxPackageBytes <= 0 ||
                maxReleaseNotesBytes < 0 ||
                maxReleaseNotesBytes >
                    UpdateManifest.ProtocolMaximumReleaseNotesBytes ||
                maxArchiveEntries < 1 ||
                maxExpandedBytes <= 0 ||
                maxCompressionRatio <= 0 ||
                float.IsNaN(maxCompressionRatio) ||
                float.IsInfinity(maxCompressionRatio))
            {
                error = UpdateValidationError.InvalidConfiguration;
                return false;
            }

            error = UpdateValidationError.None;
            return true;
        }
    }
}
