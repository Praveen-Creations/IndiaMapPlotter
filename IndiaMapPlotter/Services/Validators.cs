using System;

namespace IndiaMapPlotter.Services
{
    public static class Validators
    {
        private static readonly string[] ValidSetupTypes = { "2S", "3S", "SS" };

        public static bool IsValidLatitude(double latitude, out string? errorMessage)
        {
            if (latitude < -90 || latitude > 90)
            {
                errorMessage = $"Latitude {latitude} is out of valid range (-90 to 90)";
                return false;
            }
            errorMessage = null;
            return true;
        }

        public static bool IsValidLongitude(double longitude, out string? errorMessage)
        {
            if (longitude < -180 || longitude > 180)
            {
                errorMessage = $"Longitude {longitude} is out of valid range (-180 to 180)";
                return false;
            }
            errorMessage = null;
            return true;
        }

        public static bool IsValidSetupType(string setupType, out string? normalizedType, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(setupType))
            {
                normalizedType = null;
                errorMessage = "SetupType is empty";
                return false;
            }

            normalizedType = setupType.Trim().ToUpperInvariant();
            var tempNormalized = normalizedType;

            if (Array.Exists(ValidSetupTypes, type => type == tempNormalized))
            {
                errorMessage = null;
                return true;
            }

            errorMessage = $"Invalid SetupType '{setupType}'. Must be one of: 2S, 3S, SS";
            return false;
        }

        public static string[] GetValidSetupTypes() => (string[])ValidSetupTypes.Clone();
    }
}
