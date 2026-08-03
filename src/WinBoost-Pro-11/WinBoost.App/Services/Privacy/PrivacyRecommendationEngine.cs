using System;
using System.Collections.Generic;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Privacy
{
    public class PrivacyRecommendationEngine
    {
        public IReadOnlyList<HealthRecommendation>
            BuildRecommendations(
                IEnumerable<PrivacyCheckItem> privacyItems)
        {
            var recommendations =
                new List<HealthRecommendation>();

            foreach (PrivacyCheckItem item
                     in privacyItems)
            {
                HealthRecommendation?
                    recommendation =
                        CreateRecommendation(item);

                if (recommendation != null)
                {
                    recommendations.Add(
                        recommendation);
                }
            }

            return recommendations;
        }

        private static HealthRecommendation?
            CreateRecommendation(
                PrivacyCheckItem item)
        {
            return item.Id switch
            {
                "diagnostic-data" =>
                    CreateDiagnosticDataRecommendation(
                        item),

                "advertising-id" =>
                    CreateAdvertisingIdRecommendation(
                        item),

                "activity-history" =>
                    CreateActivityHistoryRecommendation(
                        item),

                "location-services" =>
                    CreateLocationServicesRecommendation(
                        item),

                _ =>
                    null
            };
        }

        private static HealthRecommendation
            CreateDiagnosticDataRecommendation(
                PrivacyCheckItem item)
        {
            if (item.Status.Equals(
                    "Date minime",
                    StringComparison.OrdinalIgnoreCase) ||
                item.Status.Equals(
                    "Date necesare",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new HealthRecommendation
                {
                    Title =
                        "Diagnostic și telemetrie",

                    Description =
                        "Nivelul datelor de diagnostic este configurat " +
                        "pentru un nivel redus.",

                    Impact =
                        "Windows trimite doar datele necesare pentru " +
                        "funcționarea și securitatea sistemului.",

                    Recommendation =
                        "Nu este necesară nicio modificare.",

                    PotentialGain = 0,

                    CanAutoFix = false
                };
            }

            return new HealthRecommendation
            {
                Title =
                    "Reduce datele de diagnostic",

                Description =
                    $"Nivel detectat: {item.Status}.",

                Impact =
                    "Windows poate trimite către Microsoft informații " +
                    "suplimentare despre utilizarea dispozitivului.",

                Recommendation =
                    "Configurează datele de diagnostic la nivelul " +
                    "minim disponibil pentru ediția ta de Windows.",

                PotentialGain =
                    item.Status.Equals(
                        "Date opționale",
                        StringComparison.OrdinalIgnoreCase)
                        ? 20
                        : 12,

                CanAutoFix = true
            };
        }

        private static HealthRecommendation
            CreateAdvertisingIdRecommendation(
                PrivacyCheckItem item)
        {
            if (item.Status.Equals(
                    "Dezactivat",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new HealthRecommendation
                {
                    Title =
                        "Advertising ID",

                    Description =
                        "Identificatorul pentru reclame personalizate " +
                        "este dezactivat.",

                    Impact =
                        "Aplicațiile nu pot utiliza acest identificator " +
                        "pentru personalizarea reclamelor.",

                    Recommendation =
                        "Setarea este configurată corect.",

                    PotentialGain = 0,

                    CanAutoFix = false
                };
            }

            return new HealthRecommendation
            {
                Title =
                    "Dezactivează Advertising ID",

                Description =
                    $"Stare detectată: {item.Status}.",

                Impact =
                    "Aplicațiile pot folosi un identificator unic " +
                    "pentru a personaliza reclamele.",

                Recommendation =
                    "Dezactivează Advertising ID pentru a reduce " +
                    "urmărirea activității între aplicații.",

                PotentialGain = 15,

                CanAutoFix = true
            };
        }

        private static HealthRecommendation
            CreateActivityHistoryRecommendation(
                PrivacyCheckItem item)
        {
            if (item.Status.Equals(
                    "Dezactivat",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new HealthRecommendation
                {
                    Title =
                        "Activity History",

                    Description =
                        "Istoricul activităților este dezactivat.",

                    Impact =
                        "Windows nu salvează și nu sincronizează " +
                        "istoricul activităților utilizatorului.",

                    Recommendation =
                        "Setarea este configurată corect.",

                    PotentialGain = 0,

                    CanAutoFix = false
                };
            }

            return new HealthRecommendation
            {
                Title =
                    "Dezactivează Activity History",

                Description =
                    $"Stare detectată: {item.Status}.",

                Impact =
                    "Windows poate salva informații despre activitățile " +
                    "și aplicațiile utilizate.",

                Recommendation =
                    "Dezactivează istoricul activităților dacă nu " +
                    "folosești sincronizarea între dispozitive.",

                PotentialGain =
                    item.Status.Equals(
                        "Activat",
                        StringComparison.OrdinalIgnoreCase)
                        ? 15
                        : 8,

                CanAutoFix = true
            };
        }

        private static HealthRecommendation
            CreateLocationServicesRecommendation(
                PrivacyCheckItem item)
        {
            if (item.Status.Equals(
                    "Dezactivat",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new HealthRecommendation
                {
                    Title =
                        "Location Services",

                    Description =
                        "Accesul general la locație este dezactivat.",

                    Impact =
                        "Aplicațiile nu pot accesa locația dispozitivului " +
                        "fără permisiuni suplimentare.",

                    Recommendation =
                        "Setarea este configurată pentru confidențialitate maximă.",

                    PotentialGain = 0,

                    CanAutoFix = false
                };
            }

            return new HealthRecommendation
            {
                Title =
                    "Revizuiește Location Services",

                Description =
                    $"Stare detectată: {item.Status}.",

                Impact =
                    "Aplicațiile autorizate pot utiliza locația " +
                    "dispozitivului.",

                Recommendation =
                    "Dezactivează locația dacă nu folosești aplicații " +
                    "care depind de această funcție.",

                PotentialGain =
                    item.Status.Equals(
                        "Activat",
                        StringComparison.OrdinalIgnoreCase)
                        ? 10
                        : 5,

                CanAutoFix = true
            };
        }
    }
}