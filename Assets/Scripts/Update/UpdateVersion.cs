#nullable enable

using System;
using System.Globalization;

namespace LEADKhongPhanh.Update
{
    public readonly struct UpdateVersion :
        IComparable<UpdateVersion>,
        IEquatable<UpdateVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public UpdateVersion(int major, int minor, int patch)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException(nameof(major));
            if (minor < 0)
                throw new ArgumentOutOfRangeException(nameof(minor));
            if (patch < 0)
                throw new ArgumentOutOfRangeException(nameof(patch));

            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public static bool TryParse(string value, out UpdateVersion version)
        {
            version = default;

            if (string.IsNullOrEmpty(value))
                return false;

            int firstDot = value.IndexOf('.');
            if (firstDot <= 0)
                return false;

            int secondDot = value.IndexOf('.', firstDot + 1);
            if (secondDot <= firstDot + 1 ||
                secondDot == value.Length - 1 ||
                value.IndexOf('.', secondDot + 1) >= 0)
            {
                return false;
            }

            if (!TryParseComponent(value, 0, firstDot, out int major) ||
                !TryParseComponent(
                    value,
                    firstDot + 1,
                    secondDot,
                    out int minor) ||
                !TryParseComponent(
                    value,
                    secondDot + 1,
                    value.Length,
                    out int patch))
            {
                return false;
            }

            version = new UpdateVersion(major, minor, patch);
            return true;
        }

        public int CompareTo(UpdateVersion other)
        {
            int result = Major.CompareTo(other.Major);
            if (result != 0)
                return result;

            result = Minor.CompareTo(other.Minor);
            return result != 0 ? result : Patch.CompareTo(other.Patch);
        }

        public bool Equals(UpdateVersion other)
        {
            return Major == other.Major &&
                   Minor == other.Minor &&
                   Patch == other.Patch;
        }

        public override bool Equals(object? obj)
        {
            return obj is UpdateVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Major;
                hash = (hash * 397) ^ Minor;
                return (hash * 397) ^ Patch;
            }
        }

        public override string ToString()
        {
            return Major.ToString(CultureInfo.InvariantCulture) + "." +
                   Minor.ToString(CultureInfo.InvariantCulture) + "." +
                   Patch.ToString(CultureInfo.InvariantCulture);
        }

        public static bool operator ==(
            UpdateVersion left,
            UpdateVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            UpdateVersion left,
            UpdateVersion right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(
            UpdateVersion left,
            UpdateVersion right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(
            UpdateVersion left,
            UpdateVersion right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(
            UpdateVersion left,
            UpdateVersion right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(
            UpdateVersion left,
            UpdateVersion right)
        {
            return left.CompareTo(right) >= 0;
        }

        private static bool TryParseComponent(
            string value,
            int start,
            int end,
            out int component)
        {
            component = 0;

            int length = end - start;
            if (length <= 0)
                return false;

            if (length > 1 && value[start] == '0')
                return false;

            for (int i = start; i < end; i++)
            {
                char character = value[i];
                if (character < '0' || character > '9')
                    return false;

                int digit = character - '0';
                if (component > (int.MaxValue - digit) / 10)
                    return false;

                component = component * 10 + digit;
            }

            return true;
        }
    }
}
