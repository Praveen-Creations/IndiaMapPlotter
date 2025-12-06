using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI.Wpf;
using IndiaMapPlotter.Models;

namespace IndiaMapPlotter.Services
{
    public class MapManager
    {
        private readonly MapControl _mapControl;
        private const double IndiaLatitude = 22.0;
        private const double IndiaLongitude = 78.0;
        private List<GeoPoint> _currentPoints = new();

        // Event to notify when a point is clicked
        public event Action<GeoPoint>? PointClicked;

        public MapManager(MapControl mapControl)
        {
            _mapControl = mapControl ?? throw new ArgumentNullException(nameof(mapControl));
            
            // Subscribe to mouse click events
            if (_mapControl != null)
            {
                _mapControl.MouseLeftButtonUp += MapControl_MouseLeftButtonUp;
            }
        }

        private void MapControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_mapControl.Map == null || !_currentPoints.Any()) return;

            try
            {
                // Get the click position
                var mousePosition = e.GetPosition(_mapControl);
                
                // Get the viewport
                var viewport = _mapControl.Map.Navigator.Viewport;
                var resolution = viewport.Resolution;
                
                // Convert screen to world coordinates
                var worldX = viewport.CenterX + (mousePosition.X - viewport.Width / 2) * resolution;
                var worldY = viewport.CenterY - (mousePosition.Y - viewport.Height / 2) * resolution;
                
                // Find the marker layer
                var markerLayer = _mapControl.Map.Layers.FirstOrDefault(l => l.Name == "Markers") as WritableLayer;
                if (markerLayer == null) return;

                // Search for nearby features
                // Tolerance in screen pixels converted to world units
                var toleranceInWorldUnits = resolution * 15; // 15 pixels tolerance
                var features = markerLayer.GetFeatures();
                
                GeoPoint? closestPoint = null;
                double closestDistance = double.MaxValue;
                
                foreach (var feature in features)
                {
                    if (feature.Extent == null) continue;
                    
                    var featureX = feature.Extent.Centroid.X;
                    var featureY = feature.Extent.Centroid.Y;
                    
                    // Calculate distance
                    var dx = featureX - worldX;
                    var dy = featureY - worldY;
                    var distance = Math.Sqrt(dx * dx + dy * dy);
                    
                    if (distance <= toleranceInWorldUnits && distance < closestDistance)
                    {
                        // Found a nearby feature
                        if (feature["Row"] is string rowStr && int.TryParse(rowStr, out int rowNumber))
                        {
                            var point = _currentPoints.FirstOrDefault(p => p.SourceRowNumber == rowNumber);
                            if (point != null)
                            {
                                closestPoint = point;
                                closestDistance = distance;
                            }
                        }
                    }
                }
                
                // Invoke the event with the closest point found
                if (closestPoint != null)
                {
                    PointClicked?.Invoke(closestPoint);
                }
            }
            catch (Exception)
            {
                // Silently handle click errors
            }
        }

        public void InitializeMap()
        {
            try
            {
                // Use OpenStreetMap with appropriate zoom to show state boundaries
                var osmLayer = Mapsui.Tiling.OpenStreetMap.CreateTileLayer();
                osmLayer.Name = "BaseMap";
                _mapControl.Map?.Layers.Add(osmLayer);

                // Remove all debug/logging widgets from the map
                if (_mapControl.Map != null)
                {
                    _mapControl.Map.Widgets.Clear();
                }

                var indiaCenter = SphericalMercator.FromLonLat(IndiaLongitude, IndiaLatitude);
                var centerPoint = new MPoint(indiaCenter.x, indiaCenter.y);
                
                if (_mapControl.Map?.Navigator != null)
                {
                    _mapControl.Map.Navigator.CenterOn(centerPoint);
                    // Set zoom level to show India with clear state names (closer view)
                    _mapControl.Map.Navigator.ZoomTo(1200000);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize map: {ex.Message}", "Map Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PlotPoints(IEnumerable<GeoPoint> points)
        {
            try
            {
                var pointsList = points.ToList();
                if (!pointsList.Any())
                {
                    MessageBox.Show("No points to plot.", "Info", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Store current points for click event handling
                _currentPoints = pointsList;

                var existingLayer = _mapControl.Map?.Layers.FirstOrDefault(l => l.Name == "Markers");
                if (existingLayer != null)
                {
                    _mapControl.Map?.Layers.Remove(existingLayer);
                }

                var markerLayer = CreateMarkerLayer(pointsList);
                _mapControl.Map?.Layers.Add(markerLayer);
                AutoZoomToPoints(pointsList);
                _mapControl.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to plot points: {ex.Message}", "Plot Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private WritableLayer CreateMarkerLayer(List<GeoPoint> points)
        {
            var layer = new WritableLayer
            {
                Name = "Markers",
                Style = null
            };

            foreach (var point in points)
            {
                var mercatorPoint = SphericalMercator.FromLonLat(point.Longitude, point.Latitude);
                var feature = new PointFeature(new MPoint(mercatorPoint.x, mercatorPoint.y));
                
                // Store data for click events only - no labels displayed on map
                feature["Type"] = point.SetupType;
                feature["Latitude"] = point.Latitude.ToString("F4");
                feature["Longitude"] = point.Longitude.ToString("F4");
                feature["Row"] = point.SourceRowNumber.ToString();

                var color = GetColorForSetupType(point.SetupType);
                feature.Styles.Add(new SymbolStyle
                {
                    SymbolScale = 0.35,  // Balanced size - visible but not overwhelming
                    Fill = new Brush(color),
                    Outline = new Pen(Mapsui.Styles.Color.Black, 1)
                });

                layer.Add(feature);
            }

            return layer;
        }

        private Mapsui.Styles.Color GetColorForSetupType(string setupType)
        {
            return setupType.ToUpperInvariant() switch
            {
                "2S" => Mapsui.Styles.Color.Green,
                "3S" => Mapsui.Styles.Color.Red,
                "SS" => Mapsui.Styles.Color.Blue,
                _ => Mapsui.Styles.Color.Black
            };
        }

        private void AutoZoomToPoints(List<GeoPoint> points)
        {
            if (!points.Any() || _mapControl.Map?.Navigator == null)
                return;

            try
            {
                var minLat = points.Min(p => p.Latitude);
                var maxLat = points.Max(p => p.Latitude);
                var minLng = points.Min(p => p.Longitude);
                var maxLng = points.Max(p => p.Longitude);

                var minMercator = SphericalMercator.FromLonLat(minLng, minLat);
                var maxMercator = SphericalMercator.FromLonLat(maxLng, maxLat);

                var padding = 0.1;
                var width = maxMercator.x - minMercator.x;
                var height = maxMercator.y - minMercator.y;

                var extent = new MRect(
                    minMercator.x - width * padding,
                    minMercator.y - height * padding,
                    maxMercator.x + width * padding,
                    maxMercator.y + height * padding
                );

                _mapControl.Map.Navigator.ZoomToBox(extent);
            }
            catch (Exception)
            {
                var indiaCenter = SphericalMercator.FromLonLat(IndiaLongitude, IndiaLatitude);
                _mapControl.Map?.Navigator?.CenterOn(new MPoint(indiaCenter.x, indiaCenter.y));
            }
        }

        public void ClearMarkers()
        {
            _currentPoints.Clear();
            var markerLayer = _mapControl.Map?.Layers.FirstOrDefault(l => l.Name == "Markers");
            if (markerLayer != null)
            {
                _mapControl.Map?.Layers.Remove(markerLayer);
                _mapControl.Refresh();
            }
        }
    }
}
