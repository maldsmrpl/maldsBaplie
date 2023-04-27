using EdiTools;
using LashingCalculator;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;


namespace LashingCalculator
{
    public partial class MainWindow : Window, INotifyPropertyChanged
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
                int holdCount = 0;
                int deckCount = 0;
                int dgCount = 0;
                int oogCount = 0;

                BaplieItem currentItem = null;

                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];

                    if (segment.Id.Equals("EQD", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentItem != null)
                        {
                            BaplieItems.Add(currentItem);
                            currentItem = null;
                        }

                        string containerNumber = segment.Elements.Count >= 2 ? segment.Elements[1]?.Value : null;
                        string position = null;
                        int size = 0;
                        string isoCode = null;

                        if (segment.Elements.Count >= 3)
                        {
                            isoCode = segment.Elements[2]?.Value;
                            size = GetContainerSizeFromIsoCode(isoCode);
                        }

                        currentItem = new BaplieItem { ContainerNumber = containerNumber, IsoCode = isoCode, Size = size };
                    }

                    if (currentItem == null) continue;
                    currentItem.Segments.Add(segment);

                    if (segment.Id.Equals("DIM", StringComparison.OrdinalIgnoreCase))
                    {
                        int leftOverDim = 0;
                        int rightOverDim = 0;
                        int topOverDim = 0;

                        for (int j = 0; j < segment.Elements.Count; j++)
                        {
                            int.TryParse(segment.Elements[j]?.Value, out int overDim);
                            switch (j)
                            {
                                case 0:
                                    leftOverDim = overDim;
                                    break;
                                case 1:
                                    rightOverDim = overDim;
                                    break;
                                case 2:
                                    topOverDim = overDim;
                                    break;
                            }
                        }

                        currentItem.LeftOverDim = leftOverDim;
                        currentItem.RightOverDim = rightOverDim;
                        currentItem.TopOverDim = topOverDim;

                        
                        if (leftOverDim > 0 || rightOverDim > 0 || topOverDim > 0)
                        {
                            currentItem.IsOOG = true;
                            oogCount++;
                        }
                    }

                    //if (segment.Id.Equals("FTX", StringComparison.OrdinalIgnoreCase) && segment.Elements.Count >= 4)
                    //{
                    //    decimal.TryParse(segment.Elements[3]?.Value, out decimal temperature);
                    //    currentItem.Temperature = (float)temperature;
                    //}



                    if (segment.Id.Equals("LOC", StringComparison.OrdinalIgnoreCase) && segment.Elements.Count >= 2 && segment.Elements[0]?.Value == "147")
                    {
                        string positionData = segment.Elements[1]?.Value;

                        if (positionData != null && positionData.Length == 7)
                        {
                            int.TryParse(positionData.Substring(0, 3), out int bay);
                            int.TryParse(positionData.Substring(3, 2), out int row);
                            int.TryParse(positionData.Substring(5, 2), out int tier);

                            currentItem.Position = $"{bay.ToString("00")} {row.ToString("00")} {tier.ToString("00")}";

                            int tierNumber;
                            if (tier >= 80)
                            {
                                currentItem.Location = Location.deck;
                                deckCount++;
                            }
                            else
                            {
                                currentItem.Location = Location.hold;
                                holdCount++;
                            }
                        }
                    }

                    if (segment.Id.Equals("DGS", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!currentItem.IsDG)
                        {
                            currentItem.IsDG = true;
                            dgCount++;
                        }
                    }

                }

                if (currentItem != null)
                {
                    BaplieItems.Add(currentItem);
                }
                
                TextBlockTotalNumberOfBoxes.Text = GetTotalContainerCount().ToString();
                TextBlockCountTEU.Text = CountTEU().ToString();
                TextBlockCount20FootContainers.Text = Count20FootContainers().ToString();
                TextBlockCount40FootContainers.Text = Count40FootContainers().ToString();
                TextBlockCount45FootContainers.Text = Count45FootContainers().ToString();
                TextBlockCountDeck.Text = deckCount.ToString();
                TextBlockCountHold.Text = holdCount.ToString();
                TextBlockCountDG.Text = dgCount.ToString();
                TextBlockCountRF.Text = CountRFContainers().ToString();
                TextBlockCountOOG.Text = CountOOGContainers().ToString();
            }
        }

        private int GetTotalContainerCount()
        {
            return BaplieItems.Count;
        }
        private int CountTEU()
        {
            int teuCount = 0;

            foreach (var item in BaplieItems)
            {
                switch (item.Size)
                {
                    case 20:
                        teuCount += 1;
                        break;
                    case 40:
                        teuCount += 2;
                        break;
                    case 45:
                        teuCount += 2;
                        break;
                }
            }

            return teuCount;
        }


        private int CountRFContainers()
        {
            int count = 0;

            foreach (var item in BaplieItems)
            {
                foreach (var segment in item.Segments)
                {
                    if (segment.Id.Equals("TMP", StringComparison.OrdinalIgnoreCase) && segment.Elements.Count >= 2)
                    {
                        item.Temperature = segment.Elements[1]?.Value;
                    }
                }
            }

            count = BaplieItems.Count(item => item.Temperature != null);

            return count;
        }




        private int CountOOGContainers()
        {
            return BaplieItems.Count(item => item.LeftOverDim > 0 || item.RightOverDim > 0 || item.TopOverDim > 0);
        }


        private int Count20FootContainers()
        {
            int count = 0;

            foreach (var item in BaplieItems)
            {
                if (item.Size == 20)
                {
                    count++;
                }
            }

            return count;
        }
        private int Count40FootContainers()
        {
            int count = 0;

            foreach (var item in BaplieItems)
            {
                if (item.Size == 40)
                {
                    count++;
                }
            }

            return count;
        }
        private int Count45FootContainers()
        {
            int count = 0;

            foreach (var item in BaplieItems)
            {
                if (item.Size == 45)
                {
                    count++;
                }
            }

            return count;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void SortBaplieItemsByPosition()
        {
            var sortedItems = BaplieItems.OrderBy(item => item.Position).ToList();

            BaplieItems.Clear();
            foreach (var item in sortedItems)
            {
                BaplieItems.Add(item);
            }

            OnPropertyChanged(nameof(BaplieItems));
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