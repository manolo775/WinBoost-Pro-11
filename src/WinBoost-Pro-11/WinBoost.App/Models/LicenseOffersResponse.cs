using System.Collections.Generic;

namespace WinBoost.App.Models
{
    public sealed class LicenseOffersResponse
    {
        public bool Success
        {
            get;
            init;
        }

        public List<LicenseOffer> Offers
        {
            get;
            init;
        } = new();

        public string ErrorCode
        {
            get;
            init;
        } = string.Empty;

        public string Message
        {
            get;
            init;
        } = string.Empty;
    }
}