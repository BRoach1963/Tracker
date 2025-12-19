using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.DataModels;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// A reusable Key Result item control for displaying KR details.
    /// 
    /// Features:
    /// - Displays KR title, progress bar, current/target values
    /// - Selection state with visual feedback
    /// - Action menu (Edit, Duplicate, Delete)
    /// - Configurable display options (show status, show actions)
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:KeyResultItem KeyResult="{Binding SelectedKR}" 
    ///                          IsSelected="{Binding IsSelected}"
    ///                          ItemClicked="KR_Clicked"
    ///                          EditRequested="KR_Edit"/&gt;
    /// </code>
    /// </summary>
    public partial class KeyResultItem : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// The Key Result data to display.
        /// </summary>
        public static readonly DependencyProperty KeyResultProperty =
            DependencyProperty.Register(nameof(KeyResult), typeof(KeyResult), typeof(KeyResultItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Whether this item is currently selected.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(KeyResultItem),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether to show the status badge.
        /// </summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(nameof(ShowStatus), typeof(bool), typeof(KeyResultItem),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether to show the actions menu button.
        /// </summary>
        public static readonly DependencyProperty ShowActionsProperty =
            DependencyProperty.Register(nameof(ShowActions), typeof(bool), typeof(KeyResultItem),
                new PropertyMetadata(true));

        #endregion

        #region Routed Events

        /// <summary>
        /// Event raised when the item is clicked.
        /// </summary>
        public static readonly RoutedEvent ItemClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(ItemClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(KeyResultItem));

        /// <summary>
        /// Event raised when edit is requested.
        /// </summary>
        public static readonly RoutedEvent EditRequestedEvent =
            EventManager.RegisterRoutedEvent(nameof(EditRequested), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(KeyResultItem));

        /// <summary>
        /// Event raised when duplicate is requested.
        /// </summary>
        public static readonly RoutedEvent DuplicateRequestedEvent =
            EventManager.RegisterRoutedEvent(nameof(DuplicateRequested), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(KeyResultItem));

        /// <summary>
        /// Event raised when delete is requested.
        /// </summary>
        public static readonly RoutedEvent DeleteRequestedEvent =
            EventManager.RegisterRoutedEvent(nameof(DeleteRequested), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(KeyResultItem));

        #endregion

        #region Properties

        public KeyResult? KeyResult
        {
            get => (KeyResult?)GetValue(KeyResultProperty);
            set => SetValue(KeyResultProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        public bool ShowActions
        {
            get => (bool)GetValue(ShowActionsProperty);
            set => SetValue(ShowActionsProperty, value);
        }

        #endregion

        #region Events

        public event RoutedEventHandler ItemClicked
        {
            add => AddHandler(ItemClickedEvent, value);
            remove => RemoveHandler(ItemClickedEvent, value);
        }

        public event RoutedEventHandler EditRequested
        {
            add => AddHandler(EditRequestedEvent, value);
            remove => RemoveHandler(EditRequestedEvent, value);
        }

        public event RoutedEventHandler DuplicateRequested
        {
            add => AddHandler(DuplicateRequestedEvent, value);
            remove => RemoveHandler(DuplicateRequestedEvent, value);
        }

        public event RoutedEventHandler DeleteRequested
        {
            add => AddHandler(DeleteRequestedEvent, value);
            remove => RemoveHandler(DeleteRequestedEvent, value);
        }

        #endregion

        #region Constructor

        public KeyResultItem()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't fire item click if clicking on the actions button
            if (e.OriginalSource is DependencyObject source)
            {
                var button = FindParent<Button>(source);
                if (button == ActionsButton) return;
            }

            RaiseEvent(new RoutedEventArgs(ItemClickedEvent, this));
        }

        private void ActionsButton_Click(object sender, RoutedEventArgs e)
        {
            ActionsMenu.IsOpen = true;
            e.Handled = true;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(EditRequestedEvent, this));
        }

        private void Duplicate_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(DuplicateRequestedEvent, this));
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(DeleteRequestedEvent, this));
        }

        #endregion

        #region Helpers

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T found)
                    return found;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        #endregion
    }
}

