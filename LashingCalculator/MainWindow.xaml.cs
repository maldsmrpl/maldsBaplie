using EdiTools;
using LashingCalculator;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace LashingCalculator
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<BaplieItem> BaplieItems { set; get; } = new ObservableCollection<BaplieItem>();

        public MainWindow()
        {
            InitializeComponent();
            BaplieListView.ItemsSource = BaplieItems;
        }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "EDI Files (*.edi)|*.edi|All Files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                InputTextBox.Text = openFileDialog.FileName;
                LoadEdiDataFromFile(openFileDialog.FileName);
            }
        }

        private void LoadEdiDataFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                BaplieItems.Clear();
                string ediData = File.ReadAllText(filePath);

                EdiOptions options = new EdiOptions
                {
                    SegmentTerminator = '\'',
                    ElementSeparator = '+',
                    ComponentSeparator = ':'
                };

                var ediDocument = (EdiDocument)Activator.CreateInstance(typeof(EdiDocument), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { ediData, options }, null);

                var segments = ediDocument.Segments.ToList();
                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    if (segment.Id.Equals("EQD", StringComparison.OrdinalIgnoreCase))
                    {
                        string containerNumber = segment.Elements.Count >= 2 ? segment.Elements[1]?.Value : null;
                        string position = null;
                        int size = 0;
                        string isoCode = null;

                        if (segment.Elements.Count >= 3)
                        {
                            isoCode = segment.Elements[2]?.Value;
                            size = GetContainerSizeFromIsoCode(isoCode);
                        }

                        for (int j = i + 1; j < segments.Count; j++)
                        {
                            var nextSegment = segments[j];

                            if (nextSegment.Id.Equals("LOC", StringComparison.OrdinalIgnoreCase) && nextSegment.Elements.Count >= 2 && nextSegment.Elements[0]?.Value == "147")
                            {
                                string positionData = nextSegment.Elements[1]?.Value;

                                if (positionData != null && positionData.Length == 7)
                                {
                                    string bay = positionData.Substring(0, 3);
                                    string row = positionData.Substring(3, 2);
                                    string tier = positionData.Substring(5, 2);
                                    position = $"{bay} {row} {tier}";
                                }
                                break;
                            }

                            if (nextSegment.Id.Equals("EQD", StringComparison.OrdinalIgnoreCase))
                            {
                                break;
                            }
                        }

                        if (containerNumber != null)
                        {
                            BaplieItems.Add(new BaplieItem { ContainerNumber = containerNumber, Position = position, IsoCode = isoCode, Size = size });
                        }
                    }
                }
            }
        }

        private static int GetContainerSizeFromIsoCode(string isoCode)
        {
            if (isoCode == null || isoCode.Length < 1)
            {
                return 0;
            }

            char firstDigit = isoCode[0];
            int size;

            switch (firstDigit)
            {
                case '2':
                    size = 20;
                    break;
                case '4':
                    size = 40;
                    break;
                case 'L':
                    size = 45;
                    break;
                case 'M':
                    size = 48;
                    break;
                default:
                    size = 0;
                    break;
            }

            return size;
        }
    } 
}
            

