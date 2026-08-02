using System;

namespace WinBoost.App.Services.Health
{
    public class SystemHealthCalculator
    {
        public int CalculatePerformanceScore(
            double cpuUsage,
            double ramUsage,
            double diskUsage)
        {
            int score = 100;

            if (cpuUsage > 80)
            {
                score -= 15;
            }
            else if (cpuUsage > 60)
            {
                score -= 8;
            }

            if (ramUsage > 90)
            {
                score -= 25;
            }
            else if (ramUsage > 75)
            {
                score -= 15;
            }
            else if (ramUsage > 60)
            {
                score -= 8;
            }

            if (diskUsage > 90)
            {
                score -= 20;
            }
            else if (diskUsage > 80)
            {
                score -= 10;
            }

            return NormalizeScore(score);
        }

        public int CalculateServicesScore(
            int totalServices,
            int criticalServices,
            int optionalServices,
            int lowRiskServices)
        {
            if (totalServices <= 0)
            {
                return 80;
            }

            double criticalRatio =
                criticalServices /
                (double)totalServices;

            double optionalRatio =
                optionalServices /
                (double)totalServices;

            double lowRiskRatio =
                lowRiskServices /
                (double)totalServices;

            int penalty =
                (int)Math.Round(
                    criticalRatio * 35 +
                    optionalRatio * 10 +
                    lowRiskRatio * 5);

            int score =
                100 - penalty;

            return NormalizeScore(score);
        }

        public int CalculateStartupScore(
            int totalStartupApps,
            int enabledStartupApps)
        {
            if (totalStartupApps <= 0)
            {
                return 100;
            }

            double enabledRatio =
                enabledStartupApps /
                (double)totalStartupApps;

            int score =
                100 -
                (int)Math.Round(
                    enabledRatio * 45);

            return NormalizeScore(score);
        }

        public int CalculatePrivacyScore(
            int totalChecks,
            int passedChecks)
        {
            if (totalChecks <= 0)
            {
                return 80;
            }

            double passedRatio =
                passedChecks /
                (double)totalChecks;

            int score =
                (int)Math.Round(
                    passedRatio * 100);

            return NormalizeScore(score);
        }

        public int CalculateWindowsUpdateScore(
            int pendingUpdates,
            bool requiresRestart)
        {
            int score = 100;

            score -= pendingUpdates * 8;

            if (requiresRestart)
            {
                score -= 15;
            }

            return NormalizeScore(score);
        }

        public int CalculateOverallScore(
            int performanceScore,
            int servicesScore,
            int startupScore,
            int privacyScore,
            int windowsUpdateScore)
        {
            double weightedScore =
                performanceScore * 0.30 +
                servicesScore * 0.20 +
                startupScore * 0.15 +
                privacyScore * 0.15 +
                windowsUpdateScore * 0.20;

            return NormalizeScore(
                (int)Math.Round(
                    weightedScore));
        }

        private static int NormalizeScore(
            int score)
        {
            return Math.Clamp(
                score,
                0,
                100);
        }
    }
}