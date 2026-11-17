using System.Windows;
using LabLabelPrototype.Services;

namespace LabLabelPrototype
{
    public partial class MainWindow : Window
    {
        private readonly MockSampleService _sampleService;
        private readonly LabelPrintService _printService;

        public MainWindow()
        {
            InitializeComponent();

            _sampleService = new MockSampleService();
            _printService = new LabelPrintService();
        }

        private void IncubateAndPrint_Click(
            object sender,
            RoutedEventArgs e)
        {
            var samples = _sampleService.GetSamples();

            if (samples.Count == 0)
            {
                MessageBox.Show("No incubation data found.");
                return;
            }

            var tray = samples[0];

            _printService.PrintLabel(tray);
        }
    }
}
