
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LabLabelPrototype.Models;
using System.Linq;
using System.Printing;

namespace LabLabelPrototype.Services
{
    public class LabelPrintService
    {
        // WPF uses 96 units per inch.
        // 1 millimeter = 96 / 25.4 WPF units.
        private const double Mm = 96.0 / 25.4;

        private const double LabelWidth = 50 * Mm;
        private const double LabelHeight = 20 * Mm;

        public void PrintLabel(IncubationSample tray)
        {

            PrintServer printServer = new LocalPrintServer();

            var tscPrinters = printServer
                .GetPrintQueues()
                .Where(printer =>
                    printer.Name.Contains(
                        "TSC",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (tscPrinters.Count == 0)
            {
                MessageBox.Show(
                    "No TSC printer was found on this computer.",
                    "Printer Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (tscPrinters.Count > 1)
            {
                MessageBox.Show(
                    "Multiple TSC printers were found. " +
                    "Printer selection must be configured.",
                    "Multiple Printers",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            PrintQueue selectedPrinter = tscPrinters[0];

            PrintDialog dialog = new PrintDialog
            {
                PrintQueue = selectedPrinter
            };



            // Create the label.
            Grid label = new Grid
            {
                Width = LabelWidth,
                Height = LabelHeight,
                Background = Brushes.White
            };

            // Four fields positioned at the corners.


            // TOP LEFT — Swedish weekday
            AddText(label, tray.ReadingDay,
                4, 2, 3,
                HorizontalAlignment.Left,
                VerticalAlignment.Top);

            // TOP RIGHT — Reading time and tolerance
            AddText(label,
                $"{tray.ReadingTime} ±{tray.ToleranceHours}h",
                3, 2, 2.5,
                HorizontalAlignment.Right,
                VerticalAlignment.Top);

            // BOTTOM LEFT — Incubator
            AddText(label, tray.Incubator,
                4, 2, 2.5,
                HorizontalAlignment.Left,
                VerticalAlignment.Bottom);

            // BOTTOM RIGHT — PUID
            AddText(label, tray.PUID,
                3, 2, 3,
                HorizontalAlignment.Right,
                VerticalAlignment.Bottom);


            // Prepare the label for printing.
            label.Measure(new Size(LabelWidth, LabelHeight));
            label.Arrange(new Rect(
                0, 0, LabelWidth, LabelHeight));

            label.UpdateLayout();

            // Send the label to the selected printer.
            dialog.PrintVisual(label, "Incubation Label");
        }


        private void AddText(
            Grid label,
            string value,
            double horizontalMargin,
            double verticalMargin,
            double fontSize,
            HorizontalAlignment horizontal,
            VerticalAlignment vertical)
        {
            TextBlock text = new TextBlock
            {
                Text = value,
                FontSize = fontSize * Mm,
                FontFamily = new FontFamily("Arial"),
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,

                HorizontalAlignment = horizontal,
                VerticalAlignment = vertical,

                Margin = new Thickness(
                    horizontalMargin * Mm,
                    verticalMargin * Mm,
                    horizontalMargin * Mm,
                    verticalMargin * Mm
                ),

                TextWrapping = TextWrapping.NoWrap
            };

            label.Children.Add(text);
        }
    }
}
