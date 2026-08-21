using System;

namespace WinBoost.App.Services.AppUpdate
{
    public static class SemanticVersionComparer
    {
        public static int Compare(
            string left,
            string right)
        {
            if (!TryParse(
                    left,
                    out VersionParts leftVersion))
            {
                throw new ArgumentException(
                    "Invalid version.",
                    nameof(left));
            }

            if (!TryParse(
                    right,
                    out VersionParts rightVersion))
            {
                throw new ArgumentException(
                    "Invalid version.",
                    nameof(right));
            }

            int coreComparison =
                leftVersion.Core.CompareTo(
                    rightVersion.Core);

            if (coreComparison != 0)
            {
                return coreComparison;
            }

            bool leftHasPrerelease =
                !string.IsNullOrWhiteSpace(
                    leftVersion.Prerelease);

            bool rightHasPrerelease =
                !string.IsNullOrWhiteSpace(
                    rightVersion.Prerelease);

            if (!leftHasPrerelease &&
                !rightHasPrerelease)
            {
                return 0;
            }

            if (!leftHasPrerelease)
            {
                return 1;
            }

            if (!rightHasPrerelease)
            {
                return -1;
            }

            return ComparePrerelease(
                leftVersion.Prerelease,
                rightVersion.Prerelease);
        }

        private static int ComparePrerelease(
            string left,
            string right)
        {
            string[] leftParts =
                left.Split('.');

            string[] rightParts =
                right.Split('.');

            int count =
                Math.Max(
                    leftParts.Length,
                    rightParts.Length);

            for (int index = 0;
                 index < count;
                 index++)
            {
                if (index >= leftParts.Length)
                {
                    return -1;
                }

                if (index >= rightParts.Length)
                {
                    return 1;
                }

                string leftPart =
                    leftParts[index];

                string rightPart =
                    rightParts[index];

                bool leftNumeric =
                    int.TryParse(
                        leftPart,
                        out int leftNumber);

                bool rightNumeric =
                    int.TryParse(
                        rightPart,
                        out int rightNumber);

                if (leftNumeric &&
                    rightNumeric)
                {
                    int numericComparison =
                        leftNumber.CompareTo(
                            rightNumber);

                    if (numericComparison != 0)
                    {
                        return numericComparison;
                    }

                    continue;
                }

                if (leftNumeric != rightNumeric)
                {
                    return leftNumeric
                        ? -1
                        : 1;
                }

                int textComparison =
                    string.Compare(
                        leftPart,
                        rightPart,
                        StringComparison.Ordinal);

                if (textComparison != 0)
                {
                    return textComparison;
                }
            }

            return 0;
        }

        private static bool TryParse(
            string value,
            out VersionParts result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }

            string normalized =
                value.Trim();

            int metadataIndex =
                normalized.IndexOf('+');

            if (metadataIndex >= 0)
            {
                normalized =
                    normalized.Substring(
                        0,
                        metadataIndex);
            }

            string coreText =
                normalized;

            string prerelease =
                string.Empty;

            int prereleaseIndex =
                normalized.IndexOf('-');

            if (prereleaseIndex >= 0)
            {
                coreText =
                    normalized.Substring(
                        0,
                        prereleaseIndex);

                prerelease =
                    normalized.Substring(
                        prereleaseIndex + 1);
            }

            if (!Version.TryParse(
                    coreText,
                    out Version? coreVersion))
            {
                return false;
            }

            result =
                new VersionParts(
                    coreVersion,
                    prerelease);

            return true;
        }

        private readonly struct VersionParts
        {
            public VersionParts(
                Version core,
                string prerelease)
            {
                Core = core;
                Prerelease = prerelease;
            }

            public Version Core
            {
                get;
            }

            public string Prerelease
            {
                get;
            }
        }
    }
}