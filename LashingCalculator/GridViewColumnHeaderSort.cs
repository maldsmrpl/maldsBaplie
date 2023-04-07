using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace LashingCalculator
{
    public static class GridViewColumnHeaderSort
    {
        public static bool GetIsSortable(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSortableProperty);
        }

        public static void SetIsSortable(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSortableProperty, value);
        }

        public static readonly DependencyProperty IsSortableProperty =
            DependencyProperty.RegisterAttached("IsSortable", typeof(bool), typeof(GridViewColumnHeaderSort), new PropertyMetadata(false, OnIsSortableChanged));


        private static void OnIsSortableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ListView listView = d as ListView;
            if (listView != null)
            {
                
            }
        }

        private static void Sort(ListView listView, string sortBy)
        {
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
}
