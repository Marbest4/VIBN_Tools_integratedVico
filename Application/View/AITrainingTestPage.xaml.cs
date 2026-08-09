using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace VIBN_Tools.Application.View
{
    public partial class AITrainingTestPage : UserControl
    {
        public AITrainingTestPage()
        {
            InitializeComponent();
        }

        private void ExportHeatmap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (HeatmapItemsControl == null)
                {
                    MessageBox.Show("Heatmap wurde nicht gefunden.",
                        "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                HeatmapItemsControl.UpdateLayout();

                var element = HeatmapItemsControl as FrameworkElement;

                double width = element.ActualWidth;
                double height = element.ActualHeight;

                if (width <= 0 || height <= 0)
                {
                    MessageBox.Show("Heatmap hat keine sichtbare Größe.",
                        "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double dpi = 192;
                var rtb = new RenderTargetBitmap(
                    (int)(width * dpi / 96),
                    (int)(height * dpi / 96),
                    dpi,
                    dpi,
                    PixelFormats.Pbgra32);

                var vb = new VisualBrush(element);
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(vb, null, new Rect(new Size(width, height)));
                }

                rtb.Render(dv);

                var dlg = new SaveFileDialog
                {
                    Title = "Heatmap exportieren",
                    Filter = "PNG Bild|*.png",
                    FileName = $"confusion_heatmap_{DateTime.Now:yyyyMMdd_HHmm}.png"
                };

                if (dlg.ShowDialog() == true)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using var fs = File.OpenWrite(dlg.FileName);
                    encoder.Save(fs);

                    MessageBox.Show("Heatmap erfolgreich exportiert.",
                        "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export fehlgeschlagen: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}