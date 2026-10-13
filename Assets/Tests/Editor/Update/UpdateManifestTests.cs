using LEADKhongPhanh.Update;
using NUnit.Framework;

namespace LEADKhongPhanh.Update.Tests
{
    public sealed class UpdateManifestTests
    {
        private const string ValidSha =
            "0123456789abcdef0123456789abcdef" +
            "0123456789abcdef0123456789abcdef";

        private const int MaxManifestBytes =
            UpdateManifest.ProtocolMaximumManifestBytes;

        private const int MaxReleaseNotesBytes =
            UpdateManifest.ProtocolMaximumReleaseNotesBytes;

        private const string ProductionManifestJson =
            "{\"version\":\"0.2.0\"," +
            "\"minimumSupportedVersion\":\"0.1.0\"," +
            "\"downloadUrl\":\"https://github.com/xduc199/LEAD-KHONG-PHANH/releases/download/v0.2.0/LEAD-KHONG-PHANH-v0.2.0.zip\"," +
            "\"sha256\":\"23e1fd8b7e4d359c61be0f3339b9d8ae85cab07a0b6aa31deb9c4e8cd153c4fa\"," +
            "\"fileSize\":332371328," +
            "\"releaseNotes\":\"LEAD KHÔNG PHANH v0.2.0\"}";

        [Test]
        public void TryParseManifestJson_AcceptsProductionManifestSchema()
        {
            Assert.That(ProductionManifestJson, Does.Not.Contain("\"id\""));

            bool success = JsonUpdateProvider.TryParseManifestJson(
                ProductionManifestJson,
                MaxManifestBytes,
                long.MaxValue,
                MaxReleaseNotesBytes,
                out UpdateManifest manifest,
                out UpdateValidationError error);

            Assert.That(success, Is.True);
            Assert.That(error, Is.EqualTo(UpdateValidationError.None));
            Assert.That(manifest.Version.ToString(), Is.EqualTo("0.2.0"));
            Assert.That(
                manifest.MinimumSupportedVersion.ToString(),
                Is.EqualTo("0.1.0"));
            Assert.That(
                manifest.DownloadUrl.AbsoluteUri,
                Is.EqualTo("https://github.com/xduc199/LEAD-KHONG-PHANH/releases/download/v0.2.0/LEAD-KHONG-PHANH-v0.2.0.zip"));
            Assert.That(
                manifest.Sha256,
                Is.EqualTo("23e1fd8b7e4d359c61be0f3339b9d8ae85cab07a0b6aa31deb9c4e8cd153c4fa"));
            Assert.That(manifest.FileSize, Is.EqualTo(332371328L));
            Assert.That(
                manifest.ReleaseNotes,
                Is.EqualTo("LEAD KHÔNG PHANH v0.2.0"));
            Assert.That(error, Is.Not.EqualTo(UpdateValidationError.UnknownField));
        }

        [Test]
        public void TryParseManifestJson_AcceptsValidManifest()
        {
            bool success = JsonUpdateProvider.TryParseManifestJson(
                CreateJson(),
                MaxManifestBytes,
                1024,
                MaxReleaseNotesBytes,
                out UpdateManifest manifest,
                out UpdateValidationError error);

            Assert.That(success, Is.True);
            Assert.That(error, Is.EqualTo(UpdateValidationError.None));
            Assert.That(manifest.Version.ToString(), Is.EqualTo("0.2.0"));
            Assert.That(
                manifest.MinimumSupportedVersion.ToString(),
                Is.EqualTo("0.1.0"));
            Assert.That(manifest.DownloadUrl.Scheme, Is.EqualTo("https"));
            Assert.That(manifest.Sha256, Is.EqualTo(ValidSha));
            Assert.That(manifest.FileSize, Is.EqualTo(123));
            Assert.That(manifest.ReleaseNotes, Is.EqualTo("Release notes."));
        }

        [Test]
        public void TryParseManifestJson_AllowsOmittedReleaseNotes()
        {
            string json = CreateJson(releaseNotesProperty: false);

            bool success = JsonUpdateProvider.TryParseManifestJson(
                json,
                MaxManifestBytes,
                1024,
                MaxReleaseNotesBytes,
                out UpdateManifest manifest,
                out UpdateValidationError error);

            Assert.That(success, Is.True);
            Assert.That(error, Is.EqualTo(UpdateValidationError.None));
            Assert.That(manifest.ReleaseNotes, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("{")]
        [TestCase("[]")]
        [TestCase("{\"version\":\"0.2.0\"} trailing")]
        [TestCase("{\"version\":\"0.2.0\",}")]
        public void TryParseManifestJson_RejectsMalformedJson(string json)
        {
            AssertInvalidJson(
                json,
                UpdateValidationError.MalformedJson);
        }

        [TestCase(
            "{\"version\":false}",
            UpdateValidationError.InvalidFieldType)]
        [TestCase(
            "{\"version\":\"0.2.0\",\"minimumSupportedVersion\":1}",
            UpdateValidationError.InvalidFieldType)]
        [TestCase(
            "{\"version\":\"0.2.0\",\"minimumSupportedVersion\":\"0.1.0\"," +
            "\"downloadUrl\":\"https://example.com/a.zip\"," +
            "\"sha256\":\"" + ValidSha + "\",\"fileSize\":\"123\"}",
            UpdateValidationError.InvalidFieldType)]
        public void TryParseManifestJson_RejectsMissingOrWrongTypes(
            string json,
            UpdateValidationError expectedError)
        {
            AssertInvalidJson(json, expectedError);
        }

        [Test]
        public void TryParseManifestJson_RejectsDuplicateProperties()
        {
            string json = CreateJson().Replace(
                "\"version\":\"0.2.0\"",
                "\"version\":\"0.2.0\",\"version\":\"0.2.1\"");

            AssertInvalidJson(json, UpdateValidationError.MalformedJson);
        }

        [Test]
        public void TryParseManifestJson_RejectsUnknownFields()
        {
            string json = CreateJson().Replace(
                "\"version\":\"0.2.0\"",
                "\"version\":\"0.2.0\",\"targetPath\":\"outside\"");

            AssertInvalidJson(json, UpdateValidationError.UnknownField);
        }

        [Test]
        public void TryParseManifestJson_RejectsManifestAboveByteLimit()
        {
            string json = CreateJson();

            bool success = JsonUpdateProvider.TryParseManifestJson(
                json,
                16,
                1024,
                MaxReleaseNotesBytes,
                out _,
                out UpdateValidationError error);

            Assert.That(success, Is.False);
            Assert.That(
                error,
                Is.EqualTo(UpdateValidationError.ManifestTooLarge));
        }

        [TestCase("1.0.0", "1.0.0", UpdateValidationError.None)]
        [TestCase("invalid", "0.1.0", UpdateValidationError.InvalidVersion)]
        [TestCase("0.2.0", "invalid",
            UpdateValidationError.InvalidMinimumVersion)]
        [TestCase("0.2.0", "0.3.0",
            UpdateValidationError.MinimumVersionGreaterThanLatest)]
        public void TryCreate_ValidatesVersions(
            string version,
            string minimumVersion,
            UpdateValidationError expectedError)
        {
            bool success = UpdateManifest.TryCreate(
                version,
                minimumVersion,
                "https://example.com/update.zip",
                ValidSha,
                123,
                string.Empty,
                1024,
                MaxReleaseNotesBytes,
                out _,
                out UpdateValidationError error);

            Assert.That(
                success,
                Is.EqualTo(expectedError == UpdateValidationError.None));
            Assert.That(error, Is.EqualTo(expectedError));
        }

        [TestCase("\"version\":\"0.2.0\",")]
        [TestCase("\"minimumSupportedVersion\":\"0.1.0\",")]
        [TestCase("\"downloadUrl\":\"https://example.com/update.zip\",")]
        [TestCase("\"sha256\":\"" + ValidSha + "\",")]
        [TestCase("\"fileSize\":123,")]
        public void TryParseManifestJson_RejectsEachMissingRequiredField(
            string omittedField)
        {
            string json = CreateJson().Replace(omittedField, string.Empty);

            AssertInvalidJson(json, UpdateValidationError.MissingField);
        }

        [TestCase("http://example.com/update.zip",
            UpdateValidationError.NonHttpsUrl)]
        [TestCase("ftp://example.com/update.zip",
            UpdateValidationError.NonHttpsUrl)]
        [TestCase("file:///update.zip",
            UpdateValidationError.NonHttpsUrl)]
        [TestCase("javascript:alert(1)",
            UpdateValidationError.NonHttpsUrl)]
        [TestCase("data:text/plain,update",
            UpdateValidationError.NonHttpsUrl)]
        [TestCase("https:/example.com/update.zip",
            UpdateValidationError.InvalidUrl)]
        [TestCase("https:\\\\example.com\\update.zip",
            UpdateValidationError.InvalidUrl)]
        [TestCase("https://example.com/update\n.zip",
            UpdateValidationError.InvalidUrl)]
        [TestCase("https://user:pass@example.com/update.zip",
            UpdateValidationError.UrlContainsCredentials)]
        [TestCase("not a URL", UpdateValidationError.InvalidUrl)]
        public void TryCreate_ValidatesUrl(
            string url,
            UpdateValidationError expectedError)
        {
            bool success = UpdateManifest.TryCreate(
                "0.2.0",
                "0.1.0",
                url,
                ValidSha,
                123,
                string.Empty,
                1024,
                MaxReleaseNotesBytes,
                out _,
                out UpdateValidationError error);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo(expectedError));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcde")]
        [TestCase(
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0")]
        [TestCase(
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdeg")]
        [TestCase(
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcde ")]
        public void TryCreate_RejectsInvalidSha256(string sha256)
        {
            bool success = UpdateManifest.TryCreate(
                "0.2.0",
                "0.1.0",
                "https://example.com/update.zip",
                sha256,
                123,
                string.Empty,
                1024,
                MaxReleaseNotesBytes,
                out _,
                out UpdateValidationError error);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo(UpdateValidationError.InvalidSha256));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void TryCreate_RejectsNonPositiveFileSize(long fileSize)
        {
            bool success = UpdateManifest.TryCreate(
                "0.2.0",
                "0.1.0",
                "https://example.com/update.zip",
                ValidSha,
                fileSize,
                string.Empty,
                1024,
                MaxReleaseNotesBytes,
                out _,
                out UpdateValidationError error);

            Assert.That(success, Is.False);
            Assert.That(
                error,
                Is.EqualTo(UpdateValidationError.InvalidFileSize));
        }

        [Test]
        public void TryCreate_RejectsFileLargerThanConfiguredLimit()
        {
            bool success = UpdateManifest.TryCreate(
                "0.2.0",
                "0.1.0",
                "https://example.com/update.zip",
                ValidSha,
                1025,
                string.Empty,
                1024,
                MaxReleaseNotesBytes,
                out _,
                out UpdateValidationError error);

            Assert.That(success, Is.False);
            Assert.That(
                error,
                Is.EqualTo(UpdateValidationError.FileSizeTooLarge));
        }

        [Test]
        public void TryParseManifestJson_RejectsFileSizeOverflow()
        {
            string json = CreateJson().Replace(
                "\"fileSize\":123",
                "\"fileSize\":9223372036854775808");

            AssertInvalidJson(json, UpdateValidationError.InvalidFileSize);
        }

        [Test]
        public void TryParseManifestJson_PreservesFileSizesAboveIntRange()
        {
            string json = CreateJson().Replace(
                "\"fileSize\":123",
                "\"fileSize\":2147483648");

            bool success = JsonUpdateProvider.TryParseManifestJson(
                json,
                MaxManifestBytes,
                long.MaxValue,
                MaxReleaseNotesBytes,
                out UpdateManifest manifest,
                out UpdateValidationError error);

            Assert.That(success, Is.True);
            Assert.That(error, Is.EqualTo(UpdateValidationError.None));
            Assert.That(manifest.FileSize, Is.EqualTo(2147483648L));
        }

        [Test]
        public void TryCreate_ValidatesReleaseNotesByUtf8ByteCount()
        {
            string multibyteNotes = new string('é', 5);

            bool success = UpdateManifest.TryCreate(
                "0.2.0",
                "0.1.0",
                "https://example.com/update.zip",
                ValidSha,
                123,
                multibyteNotes,
                1024,
                9,
                out _,
                out UpdateValidationError error);

            Assert.That(success, Is.False);
            Assert.That(
                error,
                Is.EqualTo(UpdateValidationError.ReleaseNotesTooLarge));
        }

        [Test]
        public void TryCreate_AcceptsUppercaseShaAndStoresCanonicalValue()
        {
            bool success = UpdateManifest.TryCreate(
                "0.2.0",
                "0.1.0",
                "https://example.com/update.zip",
                ValidSha.ToUpperInvariant(),
                123,
                null,
                1024,
                MaxReleaseNotesBytes,
                out UpdateManifest manifest,
                out UpdateValidationError error);

            Assert.That(success, Is.True);
            Assert.That(error, Is.EqualTo(UpdateValidationError.None));
            Assert.That(manifest.Sha256, Is.EqualTo(ValidSha));
            Assert.That(manifest.ReleaseNotes, Is.Empty);
        }

        private static string CreateJson(bool releaseNotesProperty = true)
        {
            string notes = releaseNotesProperty
                ? ",\"releaseNotes\":\"Release notes.\""
                : string.Empty;

            return "{\"version\":\"0.2.0\"," +
                   "\"minimumSupportedVersion\":\"0.1.0\"," +
                   "\"downloadUrl\":\"https://example.com/update.zip\"," +
                   "\"sha256\":\"" + ValidSha + "\"," +
                   "\"fileSize\":123" + notes + "}";
        }

        private static void AssertInvalidJson(
            string json,
            UpdateValidationError expectedError)
        {
            bool success = JsonUpdateProvider.TryParseManifestJson(
                json,
                MaxManifestBytes,
                1024,
                MaxReleaseNotesBytes,
                out _,
                out UpdateValidationError error);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo(expectedError));
        }
    }
}
