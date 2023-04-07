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
        private void ListView_Click(object sender, RoutedEventArgs e)
        {
            ListView listView = sender as ListView;
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;

            if (headerClicked != null && headerClicked.Column.DisplayMemberBinding != null)
            {
                string sortBy = ((Binding)headerClicked.Column.DisplayMemberBinding).Path.Path;
                ICollectionView dataView = CollectionViewSource.GetDefaultView(listView.ItemsSource);

                if (dataView != null)
                {
                    ListSortDirection direction = ListSortDirection.Ascending;
                    if (dataView.SortDescriptions.Count > 0 && dataView.SortDescriptions[0].PropertyName == sortBy)
                    {
                        direction = dataView.SortDescriptions[0].Direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                    }

                    dataView.SortDescriptions.Clear();
                    dataView.SortDescriptions.Add(new SortDescription(sortBy, direction));
                }
            }
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

                        Location location = Location.hold;

                        for (int j = i + 1; j < segments.Count; j++)
                        {
                            var nextSegment = segments[j];

                            if (nextSegment.Id.Equals("LOC", StringComparison.OrdinalIgnoreCase) && nextSegment.Elements.Count >= 2 && nextSegment.Elements[0]?.Value == "147")
                            {
                                string positionData = nextSegment.Elements[1]?.Value;

                                if (positionData != null && positionData.Length == 7)
                                {
                                    int.TryParse(positionData.Substring(0, 3), out int bay);
                                    int.TryParse(positionData.Substring(3, 2), out int row);
                                    int.TryParse(positionData.Substring(5, 2), out int tier);

                                    position = $"{bay.ToString("00")} {row.ToString("00")} {tier.ToString("00")}";

                                    int tierNumber;
                                    
                                        if (tier >= 80)
                                        {
                                            location = Location.deck;
                                        }
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
                            BaplieItems.Add(new BaplieItem { ContainerNumber = containerNumber, Position = position, IsoCode = isoCode, Size = size, Location = location });
                        }

                        //SortBaplieItemsByPosition();
                    }
                }
            }
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
            

