using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using IndiaMapPlotter.Models;

namespace IndiaMapPlotter.Services
{
    /// <summary>
    /// Reads and parses geographic points from Excel files
    /// </summary>
    public class ExcelReader
    {
        /// <summary>
        /// Reads points from Excel file with header detection
        /// </summary>
        /// <param name="filePath">Path to Excel file</param>
        /// <param name="errors">List of errors encountered during parsing</param>
        /// <returns>List of valid GeoPoints</returns>
        public IReadOnlyList<GeoPoint> ReadPoints(string filePath, out List<RowError> errors)
        {
            errors = new List<RowError>();
            var points = new List<GeoPoint>();

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);

                if (worksheet == null || !worksheet.RangeUsed().Rows().Any())
                {
                    errors.Add(new RowError 
                    { 
                        RowNumber = 0, 
                        ErrorMessage = "Worksheet is empty or could not be read" 
                    });
                    return points;
                }

                // Detect header columns
                var headerRow = worksheet.FirstRowUsed();
                var columnMapping = DetectColumns(headerRow, out var columnError);

                if (columnError != null)
                {
                    errors.Add(new RowError 
                    { 
                        RowNumber = 1, 
                        ErrorMessage = columnError 
                    });
                    return points;
                }

                // Parse data rows
                var dataRows = worksheet.RowsUsed().Skip(1); // Skip header row
                
                foreach (var row in dataRows)
                {
                    var rowNumber = row.RowNumber();
                    
                    try
                    {
                        var geoPoint = ParseRow(row, columnMapping, rowNumber, out var rowErrors);
                        
                        if (rowErrors.Count > 0)
                        {
                            errors.AddRange(rowErrors);
                        }
                        else if (geoPoint != null)
                        {
                            points.Add(geoPoint);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new RowError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = $"Unexpected error: {ex.Message}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new RowError
                {
                    RowNumber = 0,
                    ErrorMessage = $"Failed to read Excel file: {ex.Message}"
                });
            }

            return points;
        }

        /// <summary>
        /// Detects column indices for Latitude, Longitude, and SetupType
        /// </summary>
        private Dictionary<string, int> DetectColumns(IXLRow headerRow, out string? errorMessage)
        {
            var mapping = new Dictionary<string, int>();
            errorMessage = null;

            // Search for required columns (case-insensitive)
            int? latColumn = null;
            int? lngColumn = null;
            int? typeColumn = null;

            foreach (var cell in headerRow.CellsUsed())
            {
                var headerValue = cell.GetString().Trim();
                var headerUpper = headerValue.ToUpperInvariant();

                if (headerUpper == "LATITUDE")
                    latColumn = cell.Address.ColumnNumber;
                else if (headerUpper == "LONGITUDE")
                    lngColumn = cell.Address.ColumnNumber;
                else if (headerUpper == "SETUPTYPE")
                    typeColumn = cell.Address.ColumnNumber;
            }

            // Validate all required columns are found
            var missingColumns = new List<string>();
            
            if (!latColumn.HasValue)
                missingColumns.Add("Latitude");
            if (!lngColumn.HasValue)
                missingColumns.Add("Longitude");
            if (!typeColumn.HasValue)
                missingColumns.Add("SetupType");

            if (missingColumns.Count > 0)
            {
                errorMessage = $"Missing required columns: {string.Join(", ", missingColumns)}. " +
                             "Expected column names: 'Latitude', 'Longitude', 'SetupType' (case-insensitive)";
                return mapping;
            }

            mapping["Latitude"] = latColumn.Value;
            mapping["Longitude"] = lngColumn.Value;
            mapping["SetupType"] = typeColumn.Value;

            return mapping;
        }

        /// <summary>
        /// Parses a single row into a GeoPoint
        /// </summary>
        private GeoPoint? ParseRow(IXLRow row, Dictionary<string, int> columnMapping, int rowNumber, out List<RowError> rowErrors)
        {
            rowErrors = new List<RowError>();

            // Parse Latitude
            var latCell = row.Cell(columnMapping["Latitude"]);
            if (!TryParseDouble(latCell, out var latitude))
            {
                rowErrors.Add(new RowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Latitude",
                    ErrorMessage = $"Invalid latitude value: '{latCell.GetString()}'"
                });
            }
            else if (!Validators.IsValidLatitude(latitude, out var latError))
            {
                rowErrors.Add(new RowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Latitude",
                    ErrorMessage = latError!
                });
            }

            // Parse Longitude
            var lngCell = row.Cell(columnMapping["Longitude"]);
            if (!TryParseDouble(lngCell, out var longitude))
            {
                rowErrors.Add(new RowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Longitude",
                    ErrorMessage = $"Invalid longitude value: '{lngCell.GetString()}'"
                });
            }
            else if (!Validators.IsValidLongitude(longitude, out var lngError))
            {
                rowErrors.Add(new RowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Longitude",
                    ErrorMessage = lngError!
                });
            }

            // Parse SetupType
            var typeCell = row.Cell(columnMapping["SetupType"]);
            var setupTypeRaw = typeCell.GetString();
            
            if (!Validators.IsValidSetupType(setupTypeRaw, out var setupType, out var typeError))
            {
                rowErrors.Add(new RowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "SetupType",
                    ErrorMessage = typeError!
                });
            }

            // Return null if any errors occurred
            if (rowErrors.Count > 0)
                return null;

            return new GeoPoint
            {
                Latitude = latitude,
                Longitude = longitude,
                SetupType = setupType!,
                SourceRowNumber = rowNumber
            };
        }

        /// <summary>
        /// Tries to parse a cell value as a double
        /// </summary>
        private bool TryParseDouble(IXLCell cell, out double value)
        {
            if (cell.IsEmpty())
            {
                value = 0;
                return false;
            }

            if (cell.TryGetValue(out double doubleValue))
            {
                value = doubleValue;
                return true;
            }

            // Try parsing as string with invariant culture
            var cellString = cell.GetString();
            if (double.TryParse(cellString, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            value = 0;
            return false;
        }
    }
}

