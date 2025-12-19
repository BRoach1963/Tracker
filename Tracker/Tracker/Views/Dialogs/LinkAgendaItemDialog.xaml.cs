using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.DataModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog for linking an agenda item to a Task, OKR, KPI, or Project.
    /// </summary>
    public partial class LinkAgendaItemDialog : BaseWindow
    {
        private readonly List<LinkableItem> _allItems;
        private readonly AgendaItem _agendaItem;

        public (string Title, LinkedItemType Type, int Id)? SelectedItem { get; private set; }

        public LinkAgendaItemDialog(List<(string Title, LinkedItemType Type, int Id)> items, AgendaItem agendaItem)
        {
            InitializeComponent();
            _agendaItem = agendaItem;
            _allItems = items.Select(i => new LinkableItem(i.Title, i.Type, i.Id)).ToList();
            ItemsListBox.ItemsSource = _allItems;
            ItemsListBox.SelectionChanged += (s, e) => LinkButton.IsEnabled = ItemsListBox.SelectedItem != null;
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ItemsListBox.ItemsSource = _allItems;
            }
            else
            {
                ItemsListBox.ItemsSource = _allItems
                    .Where(i => i.Title.ToLower().Contains(searchText) || 
                               i.Type.ToString().ToLower().Contains(searchText))
                    .ToList();
            }
        }

        private void ItemsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemsListBox.SelectedItem is LinkableItem item)
            {
                SelectedItem = (item.Title, item.Type, item.Id);
                DialogResult = true;
                Close();
            }
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsListBox.SelectedItem is LinkableItem item)
            {
                SelectedItem = (item.Title, item.Type, item.Id);
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    /// <summary>
    /// Represents an item that can be linked to an agenda item.
    /// </summary>
    public class LinkableItem
    {
        public string Title { get; }
        public LinkedItemType Type { get; }
        public int Id { get; }

        public LinkableItem(string title, LinkedItemType type, int id)
        {
            Title = title;
            Type = type;
            Id = id;
        }
    }
}


