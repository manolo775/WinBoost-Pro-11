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
                        // Fișierul există, dar nu conține
                        // un rezultat valid.
                        TryDeleteResultFile();

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
                catch (JsonException)
                {
                    // JSON corupt sau incomplet.
                    // Nu îl păstrăm pentru următoarea pornire.
                    TryDeleteResultFile();

                    return null;
                }
                catch (NotSupportedException)
                {
                    // Structura JSON nu mai poate fi
                    // interpretată de versiunea curentă.
                    TryDeleteResultFile();

                    return null;
                }
                catch (IOException)
                {
                    // Poate fi o problemă temporară
                    // de acces la fișier.
                    // Îl păstrăm pentru următoarea pornire.
                    return null;
                }
                catch (UnauthorizedAccessException)
                {
                    // Nu ștergem rezultatul dacă Windows
                    // nu permite momentan accesul.
                    return null;
                }
                catch
                {
                    // Orice altă problemă legată de
                    // rezultat nu trebuie să blocheze WinBoost.
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