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

        public MapManager(MapControl mapControl)
        {
            _mapControl = mapControl ?? throw new ArgumentNullException(nameof(mapControl));
        }

        public void InitializeMap()
        {
            try
            {
                var osmLayer = Mapsui.Tiling.OpenStreetMap.CreateTileLayer();
                _mapControl.Map?.Layers.Add(osmLayer);

                var indiaCenter = SphericalMercator.FromLonLat(IndiaLongitude, IndiaLatitude);
                var centerPoint = new MPoint(indiaCenter.x, indiaCenter.y);
                
                if (_mapControl.Map?.Navigator != null)
                {
                    _mapControl.Map.Navigator.CenterOn(centerPoint);
                    _mapControl.Map.Navigator.ZoomTo(500000);
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
                
                feature["Type"] = point.SetupType;
                feature["Latitude"] = point.Latitude.ToString("F4");
                feature["Longitude"] = point.Longitude.ToString("F4");
                feature["Row"] = point.SourceRowNumber.ToString();
                feature["Label"] = point.DisplayInfo;

                var color = GetColorForSetupType(point.SetupType);
                feature.Styles.Add(new SymbolStyle
                {
                    SymbolScale = 0.8,
                    Fill = new Brush(color),
                    Outline = new Pen(Mapsui.Styles.Color.Black, 2)
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
            var markerLayer = _mapControl.Map?.Layers.FirstOrDefault(l => l.Name == "Markers");
            if (markerLayer != null)
            {
                _mapControl.Map?.Layers.Remove(markerLayer);
                _mapControl.Refresh();
            }
        }
    }
}
