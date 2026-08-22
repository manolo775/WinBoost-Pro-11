using System;
using System.IO;
using System.Text.Json;
using WinBoost.App.Models;

namespace WinBoost.App.Services.AppUpdate
{
    public sealed class WinBoostSelfUpdateResultService
    {
        private static readonly object SyncRoot =
            new object();

        private static readonly string ResultFilePath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "WinBoost",
                "Update",
                "last-update-result.json");

        private static bool _resultWasLoaded;

        private static WinBoostSelfUpdateResult?
            _cachedResult;

        public WinBoostSelfUpdateResult?
            ReadAndConsume()
        {
            lock (SyncRoot)
            {
                // Dacă rezultatul a fost deja citit
                // în această sesiune WinBoost,
                // îl returnăm din memorie.
                if (_resultWasLoaded)
                {
                    return _cachedResult;
                }

                _resultWasLoaded =
                    true;

                if (!File.Exists(
                        ResultFilePath))
                {
                    return null;
                }

                try
                {
                    string json =
                        File.ReadAllText(
                            ResultFilePath);

                    WinBoostSelfUpdateResult? result =
                        JsonSerializer.Deserialize<
                            WinBoostSelfUpdateResult>(
                            json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive =
                                    true
                            });

                    if (result == null)
                    {
                        return null;
                    }

                    // Păstrăm rezultatul în memorie
                    // pentru toate instanțele ViewModel
                    // din sesiunea curentă.
                    _cachedResult =
                        result;

                    // Fișierul trebuie afișat doar
                    // la prima pornire după update.
                    TryDeleteResultFile();

                    return _cachedResult;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static void TryDeleteResultFile()
        {
            try
            {
                if (File.Exists(
                        ResultFilePath))
                {
                    File.Delete(
                        ResultFilePath);
                }
            }
            catch
            {
                // Eșecul ștergerii rezultatului
                // nu trebuie să blocheze WinBoost.
            }
        }
    }
}