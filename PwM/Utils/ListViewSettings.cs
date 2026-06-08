using System.ComponentModel;
using System.Windows.Controls;

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
            listViewItem.IsSelected = !reset;
        }

        /// <summary>
        /// Set Color of listView on click. 
        /// </summary>
        /// <param name="listViewItem"></param>
        /// <param name="reset"></param>
        public static void SetListViewColorApp(ListViewItem listViewItem, bool reset)
        {
            listViewItem.IsSelected = listViewItem.IsEnabled && !reset;
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
