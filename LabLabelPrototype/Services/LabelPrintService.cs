
using System;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LabLabelPrototype.Models;

namespace LabLabelPrototype.Services
{
    public class LabelPrintService
    {
        // WPF measures lengths in 1/96-inch units.
        private const double Mm = 96.0 / 25.4;
        private const double Pt = 96.0 / 72.0;

        // Measurements from the working Word document.
        private const double LabelWidth = 50 * Mm;
        private const double LabelHeight = 20 * Mm;

        private const double LeftMargin = 3.5 * Mm;
        private const double RightMargin = 2.0 * Mm;
        private const double TopMargin = 1.5 * Mm;
        private const double BottomMargin = 2.5 * Mm;


        // Font size in points, matching the compact Word style.
        private const double FontSize = 8.5 * Pt;

        public void PrintLabel(IncubationSample tray)
        {
            if (tray == null)
            {
                MessageBox.Show("No incubation data was provided.");
                return;
            }

            try
            {
                // STEP 1: Find the installed TSC printer.
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

                PrintQueue printer = tscPrinters[0];

                // STEP 2: Prepare the printer's page settings.
                PrintTicket baseTicket =
                    printer.UserPrintTicket ??
                    printer.DefaultPrintTicket ??
                    new PrintTicket();

                PrintTicket requestedTicket = baseTicket.Clone();

                // Prefer an existing 50 x 20 mm paper size
                // advertised by the printer driver.
                var supportedSize = printer
                    .GetPrintCapabilities()
                    .PageMediaSizeCapability
                    .FirstOrDefault(IsCorrectLabelSize);

                requestedTicket.PageMediaSize =
                    supportedSize ??
                    new PageMediaSize(LabelWidth, LabelHeight);

                // Request exactly one copy.
                requestedTicket.CopyCount = 1;

                // Ask the driver to validate the requested settings.
                var validation = printer.MergeAndValidatePrintTicket(
                    baseTicket,
                    requestedTicket);

                PrintTicket finalTicket =
                    validation.ValidatedPrintTicket;

                // IMPORTANT:
                // Do not print if the driver changed the page size.
                if (!IsCorrectLabelSize(finalTicket.PageMediaSize))
                {
                    MessageBox.Show(
                        "The printer driver did not accept the " +
                        "50 x 20 mm page size.\n\n" +
                        "Driver-selected size: " +
                        DescribePageSize(finalTicket.PageMediaSize) +
                        "\n\nConfigure a 50 x 20 mm paper size " +
                        "in the TSC printer settings before retrying.",
                        "Incorrect Label Size",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // STEP 3: Create a print dialog internally.
                // No dialog window is displayed to the user.
                PrintDialog dialog = new PrintDialog
                {
                    PrintQueue = printer,
                    PrintTicket = finalTicket
                };

                // STEP 4: Create exactly one document page.
                FixedDocument document = CreateLabelDocument(tray);

                // STEP 5: Send that page to the printer.
                dialog.PrintDocument(
                    document.DocumentPaginator,
                    "Incubation Label");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The label could not be printed.\n\n" +
                    ex.Message,
                    "Printing Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static FixedDocument CreateLabelDocument(
            IncubationSample tray)
        {
            // Physical page: 50 x 20 mm.
            FixedPage page = new FixedPage
            {
                Width = LabelWidth,
                Height = LabelHeight,
                Background = Brushes.White
            };

            // Content area inside the Word-style margins.
            Grid layout = new Grid
            {
                Width = LabelWidth - LeftMargin - RightMargin,
                Height = LabelHeight - TopMargin - BottomMargin
            };

            // Two rows: upper and lower information.
            layout.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1, GridUnitType.Star)
                });

            layout.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1, GridUnitType.Star)
                });

            // Two columns.
            // Slightly more space on the left for incubator names.
            layout.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(
                        58, GridUnitType.Star)
                });

            layout.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(
                        42, GridUnitType.Star)
                });

            // TOP LEFT: Swedish reading day.
            AddText(
                layout,
                tray.ReadingDay,
                0, 0,
                HorizontalAlignment.Left,
                VerticalAlignment.Top);

            // TOP RIGHT: Reading time and tolerance.
            AddText(
                layout,
                $"{tray.ReadingTime} ±{tray.ToleranceHours}h",
                0, 1,
                HorizontalAlignment.Right,
                VerticalAlignment.Top);

            // BOTTOM LEFT: Incubator.
            AddText(
                layout,
                tray.Incubator,
                1, 0,
                HorizontalAlignment.Left,
                VerticalAlignment.Bottom);

            // BOTTOM RIGHT: Personnel ID.
            AddText(
                layout,
                tray.PUID,
                1, 1,
                HorizontalAlignment.Right,
                VerticalAlignment.Bottom);

            // Position the content within the page margins.
            FixedPage.SetLeft(layout, LeftMargin);
            FixedPage.SetTop(layout, TopMargin);

            page.Children.Add(layout);

            // Create a document with exactly one page.
            FixedDocument document = new FixedDocument();

            document.DocumentPaginator.PageSize =
                new Size(LabelWidth, LabelHeight);

            PageContent pageContent = new PageContent
            {
                Child = page
            };

            document.Pages.Add(pageContent);

            // Prepare the page layout.
            page.Measure(new Size(LabelWidth, LabelHeight));

            page.Arrange(new Rect(
                0, 0,
                LabelWidth,
                LabelHeight));

            page.UpdateLayout();

            return document;
        }

        private static void AddText(
            Grid layout,
            string value,
            int row,
            int column,
            HorizontalAlignment horizontal,
            VerticalAlignment vertical)
        {
            TextBlock text = new TextBlock
            {
                Text = value ?? string.Empty,
                FontSize = FontSize,
                FontFamily = new FontFamily("Arial"),
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,

                HorizontalAlignment = horizontal,
                VerticalAlignment = vertical,

                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.None
            };

            Grid.SetRow(text, row);
            Grid.SetColumn(text, column);

            layout.Children.Add(text);
        }

        private static bool IsCorrectLabelSize(
            PageMediaSize size)
        {
            if (size == null ||
                !size.Width.HasValue ||
                !size.Height.HasValue)
            {
                return false;
            }

            // Allow up to 1 mm difference for driver rounding.
            double tolerance = 1.0 * Mm;

            return
                Math.Abs(size.Width.Value - LabelWidth)
                    <= tolerance &&
                Math.Abs(size.Height.Value - LabelHeight)
                    <= tolerance;
        }

        private static string DescribePageSize(
            PageMediaSize size)
        {
            if (size == null ||
                !size.Width.HasValue ||
                !size.Height.HasValue)
            {
                return "Unknown";
            }

            return
                $"{size.Width.Value / Mm:0.##} x " +
                $"{size.Height.Value / Mm:0.##} mm";
        }
    }
}
