namespace IndiaMapPlotter.Models
{
    /// <summary>
    /// Represents a geographic point with coordinates and setup type
    /// </summary>
    public class GeoPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string SetupType { get; set; } = string.Empty;
        public int SourceRowNumber { get; set; }

        /// <summary>
        /// Gets a formatted string representation for display
        /// </summary>
        public string DisplayInfo => $"Type: {SetupType}\nLat: {Latitude:F4}\nLng: {Longitude:F4}\nRow: {SourceRowNumber}";

        public override string ToString()
        {
            return $"Row {SourceRowNumber}: ({Latitude:F4}, {Longitude:F4}) - {SetupType}";
        }
    }
}

