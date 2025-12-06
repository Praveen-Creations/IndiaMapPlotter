using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using IndiaMapPlotter.Models;
using IndiaMapPlotter.Services;

namespace IndiaMapPlotter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MapManager _mapManager;
        private readonly ExcelReader _excelReader;
        private readonly ExportService _exportService;
        private List<GeoPoint> _loadedPoints = new();
        private List<RowError> _loadedErrors = new();

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize services
            _mapManager = new MapManager(MapControl);
            _excelReader = new ExcelReader();
            _exportService = new ExportService();

            // Initialize map
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _mapManager.InitializeMap();
                UpdateStatus("Map initialized. Ready to load data.", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize map: {ex.Message}", "Initialization Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles Load Excel button click
        /// </summary>
        private async void LoadExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                Title = "Select Excel File with Geographic Data"
            };

            if (dialog.ShowDialog() == true)
            {
                await LoadExcelFileAsync(dialog.FileName);
            }
        }

        /// <summary>
        /// Loads and processes Excel file asynchronously
        /// </summary>
        private async Task LoadExcelFileAsync(string filePath)
        {
            try
            {
                // Show loading overlay
                ShowLoading(true, "Loading Excel file...");
                UpdateStatus("Reading Excel file...", false);

                // Read Excel on background thread
                var result = await Task.Run(() =>
                {
                    var points = _excelReader.ReadPoints(filePath, out var errors);
                    return new { Points = points, Errors = errors };
                });

                _loadedPoints = result.Points.ToList();
                _loadedErrors = result.Errors;

                // Update UI
                UpdateDataGrid();
                UpdateErrorDisplay();
                UpdateCounters();

                // Check if we should proceed with plotting
                if (_loadedErrors.Count > 0)
                {
                    var errorSummary = $"Found {_loadedErrors.Count} error(s) in the file.\n" +
                                     $"Valid rows: {_loadedPoints.Count}\n" +
                                     $"Invalid rows: {_loadedErrors.Count}\n\n" +
                                     "Do you want to continue and plot the valid points?";

                    var result2 = MessageBox.Show(errorSummary, "Data Validation Warnings",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result2 == MessageBoxResult.No)
                    {
                        UpdateStatus("Load cancelled by user.", true);
                        ShowLoading(false);
                        return;
                    }
                }

                // Plot points if we have any
                if (_loadedPoints.Count > 0)
                {
                    UpdateStatus("Plotting markers on map...", false);
                    await Task.Delay(100); // Small delay to update UI

                    await Task.Run(() =>
                    {
                        Dispatcher.Invoke(() => _mapManager.PlotPoints(_loadedPoints));
                    });

                    UpdateStatus($"Successfully loaded {_loadedPoints.Count} point(s).", false);
                    
                    // Enable export and clear buttons
                    ExportMapButton.IsEnabled = true;
                    ClearButton.IsEnabled = true;
                }
                else
                {
                    MessageBox.Show("No valid data points found in the Excel file.", "No Data",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateStatus("No valid points to plot.", true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Excel file:\n\n{ex.Message}", "Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Failed to load Excel file.", true);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Handles Export Map button click
        /// </summary>
        private void ExportMap_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png",
                FileName = $"IndiaMap_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                Title = "Export Map as Image"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    UpdateStatus("Exporting map...", false);
                    
                    var success = _exportService.ExportToPng(MapControl, dialog.FileName);
                    
                    if (success)
                    {
                        UpdateStatus($"Map exported successfully.", false);
                    }
                    else
                    {
                        UpdateStatus("Map export failed.", true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed:\n\n{ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus("Export failed.", true);
                }
            }
        }

        /// <summary>
        /// Handles Clear button click
        /// </summary>
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all markers from the map?",
                "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _mapManager.ClearMarkers();
                _loadedPoints.Clear();
                _loadedErrors.Clear();
                
                DataGrid.ItemsSource = null;
                ErrorExpander.Visibility = Visibility.Collapsed;
                
                UpdateCounters();
                UpdateStatus("Map cleared.", false);
                
                ExportMapButton.IsEnabled = false;
                ClearButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Updates the data grid with loaded points
        /// </summary>
        private void UpdateDataGrid()
        {
            DataGrid.ItemsSource = _loadedPoints;
        }

        /// <summary>
        /// Updates the error display panel
        /// </summary>
        private void UpdateErrorDisplay()
        {
            if (_loadedErrors.Count > 0)
            {
                ErrorExpander.Visibility = Visibility.Visible;
                ErrorExpander.IsExpanded = true;

                var errorText = string.Join("\n", _loadedErrors.Select(e => e.ToString()));
                ErrorTextBlock.Text = errorText;

                // Limit display to first 100 errors
                if (_loadedErrors.Count > 100)
                {
                    ErrorTextBlock.Text += $"\n\n... and {_loadedErrors.Count - 100} more errors.";
                }
            }
            else
            {
                ErrorExpander.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Updates the status bar counters
        /// </summary>
        private void UpdateCounters()
        {
            PointsCountText.Text = _loadedPoints.Count.ToString();
            ErrorsCountText.Text = _loadedErrors.Count.ToString();
        }

        /// <summary>
        /// Updates the status bar message
        /// </summary>
        private void UpdateStatus(string message, bool isError)
        {
            StatusText.Text = message;
            StatusText.Foreground = isError 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 82, 82))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        }

        /// <summary>
        /// Shows or hides the loading overlay
        /// </summary>
        private void ShowLoading(bool show, string message = "Loading...")
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            LoadingText.Text = message;
            
            // Disable buttons while loading
            LoadExcelButton.IsEnabled = !show;
            ExportMapButton.IsEnabled = !show && _loadedPoints.Count > 0;
            ClearButton.IsEnabled = !show && _loadedPoints.Count > 0;
        }
    }
}
