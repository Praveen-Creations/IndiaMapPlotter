using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Mapsui.UI.Wpf;

namespace IndiaMapPlotter.Services
{
    /// <summary>
    /// Provides functionality to export maps to image files
    /// </summary>
    public class ExportService
    {
        /// <summary>
        /// Exports the map control to a PNG file
        /// </summary>
        /// <param name="mapControl">The map control to export</param>
        /// <param name="filePath">Output file path</param>
        /// <param name="width">Image width (optional, uses control width if not specified)</param>
        /// <param name="height">Image height (optional, uses control height if not specified)</param>
        public bool ExportToPng(MapControl mapControl, string filePath, int? width = null, int? height = null)
        {
            try
            {
                if (mapControl == null)
                {
                    MessageBox.Show("Map control is null.", "Export Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                var actualWidth = width ?? (int)mapControl.ActualWidth;
                var actualHeight = height ?? (int)mapControl.ActualHeight;

                if (actualWidth <= 0 || actualHeight <= 0)
                {
                    MessageBox.Show("Invalid map dimensions.", "Export Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Create render target bitmap
                var renderBitmap = new RenderTargetBitmap(
                    actualWidth,
                    actualHeight,
                    96d, // DPI X
                    96d, // DPI Y
                    PixelFormats.Pbgra32);

                // Render the map control
                mapControl.Measure(new Size(actualWidth, actualHeight));
                mapControl.Arrange(new Rect(new Size(actualWidth, actualHeight)));
                renderBitmap.Render(mapControl);

                // Save to PNG file
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                MessageBox.Show($"Map exported successfully to:\n{filePath}", "Export Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export map: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Exports the map with a high-resolution setting
        /// </summary>
        public bool ExportToHighResPng(MapControl mapControl, string filePath, int scale = 2)
        {
            var width = (int)mapControl.ActualWidth * scale;
            var height = (int)mapControl.ActualHeight * scale;
            return ExportToPng(mapControl, filePath, width, height);
        }
    }
}

