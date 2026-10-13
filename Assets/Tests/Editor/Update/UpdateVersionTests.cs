using System;
using LEADKhongPhanh.Update;
using NUnit.Framework;

namespace LEADKhongPhanh.Update.Tests
{
    public sealed class UpdateVersionTests
    {
        [TestCase("0.0.0")]
        [TestCase("0.1.0")]
        [TestCase("0.1.1")]
        [TestCase("1.0.0")]
        [TestCase("10.20.30")]
        public void TryParse_AcceptsSupportedVersions(string value)
        {
            Assert.That(UpdateVersion.TryParse(value, out _), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("1")]
        [TestCase("1.0")]
        [TestCase("1.0.0.0")]
        [TestCase("v1.0.0")]
        [TestCase("1.0.0-beta")]
        [TestCase("1.0.0+build")]
        [TestCase("1.a.0")]
        [TestCase("-1.0.0")]
        [TestCase("1.-1.0")]
        [TestCase("1.0.-1")]
        [TestCase(" 1.0.0")]
        [TestCase("1.0.0 ")]
        [TestCase("01.02.03")]
        [TestCase("2147483648.0.0")]
        [TestCase("0.2147483648.0")]
        [TestCase("0.0.2147483648")]
        public void TryParse_RejectsUnsupportedVersions(string value)
        {
            Assert.That(UpdateVersion.TryParse(value, out _), Is.False);
        }

        [TestCase("0.1.0", "0.1.0", 0)]
        [TestCase("0.1.0", "0.1.1", -1)]
        [TestCase("0.1.1", "0.1.0", 1)]
        [TestCase("0.1.9", "0.2.0", -1)]
        [TestCase("0.9.9", "1.0.0", -1)]
        [TestCase("1.10.0", "1.9.0", 1)]
        [TestCase("1.0.0", "10.0.0", -1)]
        public void CompareTo_UsesNumericComponents(
            string leftText,
            string rightText,
            int expectedSign)
        {
            Assert.That(
                UpdateVersion.TryParse(leftText, out UpdateVersion left),
                Is.True);
            Assert.That(
                UpdateVersion.TryParse(rightText, out UpdateVersion right),
                Is.True);

            Assert.That(
                left.CompareTo(right),
                Is.EqualTo(expectedSign));
        }

        [Test]
        public void Equality_OperatorsAndHashCodeAreConsistent()
        {
            UpdateVersion.TryParse("1.10.0", out UpdateVersion first);
            UpdateVersion.TryParse("1.10.0", out UpdateVersion same);
            UpdateVersion.TryParse("1.9.0", out UpdateVersion different);

            Assert.That(first.Equals(same), Is.True);
            Assert.That(first.Equals((object)same), Is.True);
            Assert.That(
                ((IComparable<UpdateVersion>)first).CompareTo(different),
                Is.GreaterThan(0));
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first == same, Is.True);
            Assert.That(first != different, Is.True);
            Assert.That(first > different, Is.True);
            Assert.That(different < first, Is.True);
            Assert.That(first >= same, Is.True);
            Assert.That(first <= same, Is.True);
        }

        [Test]
        public void ToString_UsesCanonicalFormat()
        {
            UpdateVersion.TryParse("10.20.30", out UpdateVersion version);

            Assert.That(version.ToString(), Is.EqualTo("10.20.30"));
        }
    }
}
