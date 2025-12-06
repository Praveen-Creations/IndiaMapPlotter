namespace IndiaMapPlotter.Models
{
    /// <summary>
    /// Represents an error that occurred while parsing a row from Excel
    /// </summary>
    public class RowError
    {
        public int RowNumber { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? ColumnName { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(ColumnName) 
                ? $"Row {RowNumber}: {ErrorMessage}"
                : $"Row {RowNumber} [{ColumnName}]: {ErrorMessage}";
        }
    }
}

