using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.DataModels;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// A reusable OKR card control for displaying objective summary.
    /// 
    /// Features:
    /// - Displays OKR title, status, progress, and key results preview
    /// - Selection state with visual feedback
    /// - Action menu (Edit, Duplicate, Delete)
    /// - Click event for selection
    /// - Configurable display options
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:OkrCard Okr="{Binding SelectedOkr}" 
    ///                    IsSelected="{Binding IsSelected}"
    ///                    CardClicked="OkrCard_Clicked"
    ///                    EditRequested="OkrCard_Edit"/&gt;
    /// </code>
    /// </summary>
    public partial class OkrCard : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// The OKR data to display.
        /// </summary>
        public static readonly DependencyProperty OkrProperty =
            DependencyProperty.Register(nameof(Okr), typeof(ObjectiveKeyResult), typeof(OkrCard),
                new PropertyMetadata(null));

        /// <summary>
        /// Whether this card is currently selected.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(OkrCard),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether to show the key results preview list.
        /// </summary>
        public static readonly DependencyProperty ShowKeyResultsProperty =
            DependencyProperty.Register(nameof(ShowKeyResults), typeof(bool), typeof(OkrCard),
                new PropertyMetadata(true));

        #endregion

        #region Routed Events

        /// <summary>
        /// Event raised when the card is clicked.
        /// </summary>
        public static readonly RoutedEvent CardClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(CardClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(OkrCard));

        /// <summary>
        /// Event raised when edit is requested.
        /// </summary>
        public static readonly RoutedEvent EditRequestedEvent =
            EventManager.RegisterRoutedEvent(nameof(EditRequested), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(OkrCard));

        /// <summary>
        /// Event raised when duplicate is requested.
        /// </summary>
        public static readonly RoutedEvent DuplicateRequestedEvent =
            EventManager.RegisterRoutedEvent(nameof(DuplicateRequested), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(OkrCard));

        /// <summary>
        /// Event raised when delete is requested.
        /// </summary>
        public static readonly RoutedEvent DeleteRequestedEvent =
            EventManager.RegisterRoutedEvent(nameof(DeleteRequested), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(OkrCard));

        #endregion

        #region Properties

        public ObjectiveKeyResult? Okr
        {
            get => (ObjectiveKeyResult?)GetValue(OkrProperty);
            set => SetValue(OkrProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public bool ShowKeyResults
        {
            get => (bool)GetValue(ShowKeyResultsProperty);
            set => SetValue(ShowKeyResultsProperty, value);
        }

        #endregion

        #region Events

        public event RoutedEventHandler CardClicked
        {
            add => AddHandler(CardClickedEvent, value);
            remove => RemoveHandler(CardClickedEvent, value);
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

        public OkrCard()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't fire card click if clicking on the actions button
            if (e.OriginalSource is DependencyObject source)
            {
                var button = FindParent<Button>(source);
                if (button == ActionsButton) return;
            }

            RaiseEvent(new RoutedEventArgs(CardClickedEvent, this));
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

