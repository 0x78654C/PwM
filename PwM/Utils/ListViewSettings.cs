using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace PwM.Utils
{
    public class ListViewSettings
    {
        /// <summary>
        /// Set Color of listView on click. 
        /// </summary>
        /// <param name="listViewItem"></param>
        /// <param name="reset"></param>
        public static void SetListViewColor(ListViewItem listViewItem, bool reset)
        {
            var converter = new BrushConverter();
            if (reset)
            {
                listViewItem.Background = Brushes.Transparent;
                listViewItem.Foreground = (Brush)converter.ConvertFromString("#FFDCDCDC");
                return;
            }
            listViewItem.Background = (Brush)converter.ConvertFromString("#6f2be3");
        }

        /// <summary>
        /// Set Color of listView on click. 
        /// </summary>
        /// <param name="listViewItem"></param>
        /// <param name="reset"></param>
        public static void SetListViewColorApp(ListViewItem listViewItem, bool reset)
        {
            if (listViewItem.IsEnabled)
            {
                var converter = new BrushConverter();
                if (reset)
                {
                    listViewItem.Background = Brushes.Transparent;
                    listViewItem.Foreground = (Brush)converter.ConvertFromString("#FFDCDCDC");
                    return;
                }
                listViewItem.Background = (Brush)converter.ConvertFromString("#6f2be3");
            }
        }

        /// <summary>
        /// List view sorting ascending by column name.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="liveSort"></param>
        public static void ListViewSortSetting(ListView listView,string columnName, bool liveSort)
        {
            listView.Items.SortDescriptions.Add(new SortDescription(columnName, ListSortDirection.Ascending));
            listView.Items.IsLiveSorting = liveSort;
            listView.Items.LiveSortingProperties.Add(columnName);
        }
    }
}
