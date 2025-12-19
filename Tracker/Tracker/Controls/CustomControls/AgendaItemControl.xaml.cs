using System.Windows;
using System.Windows.Controls;
using Tracker.DataModels;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// User control for displaying and editing an agenda item.
    /// Supports inline editing, category/priority selection, and entity linking.
    /// </summary>
    public partial class AgendaItemControl : UserControl
    {
        #region Events

        /// <summary>
        /// Raised when the delete button is clicked.
        /// </summary>
        public event EventHandler<AgendaItem>? DeleteRequested;

        /// <summary>
        /// Raised when the add link button is clicked.
        /// </summary>
        public event EventHandler<AgendaItem>? AddLinkRequested;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// The agenda item being displayed/edited.
        /// </summary>
        public static readonly DependencyProperty AgendaItemProperty =
            DependencyProperty.Register(nameof(AgendaItem), typeof(AgendaItem), typeof(AgendaItemControl),
                new PropertyMetadata(null, OnAgendaItemChanged));

        public AgendaItem? AgendaItem
        {
            get => (AgendaItem?)GetValue(AgendaItemProperty);
            set => SetValue(AgendaItemProperty, value);
        }

        private static void OnAgendaItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AgendaItemControl)d;
            control.DataContext = e.NewValue;
        }

        #endregion

        public AgendaItemControl()
        {
            InitializeComponent();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AgendaItem item)
            {
                DeleteRequested?.Invoke(this, item);
            }
        }

        private void AddLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AgendaItem item)
            {
                AddLinkRequested?.Invoke(this, item);
            }
        }
    }
}

