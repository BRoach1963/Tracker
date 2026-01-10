using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tracker.DataModels;
using Tracker.Interfaces;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// A reusable control for displaying linked measurables (KPIs, Projects, TaskCollections).
    /// 
    /// Features:
    /// - Auto-detects type and shows appropriate icon
    /// - Displays name and current progress/value
    /// - Optional remove button
    /// - Works with IMeasurable interface or raw values
    /// 
    /// Usage:
    /// <code>
    /// &lt;controls:MeasurableItem Measurable="{Binding LinkedKpi}" CanRemove="True" RemoveClicked="Remove_Measurable"/&gt;
    /// &lt;controls:MeasurableItem MeasurableType="Kpi" DisplayName="NPS Score" DisplayValue="53/60"/&gt;
    /// </code>
    /// </summary>
    public partial class MeasurableItem : UserControl
    {
        #region Static Resources

        // Icon paths for different measurable types
        private const string KpiIconPath = "M16,11.78L20.24,4.45L21.97,5.45L16.74,14.5L10.23,10.75L5.46,19H22V21H2V3H4V17.54L9.5,8L16,11.78Z";
        private const string ProjectIconPath = "M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3M19,5V19H5V5H19M17,17H7V7H17V17M15,9H9V15H15V9Z";
        private const string TaskCollectionIconPath = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M13,13V18H10V13H7L12,8L17,13H13Z";

        // Colors for different types
        private static readonly SolidColorBrush KpiBrush = new(Color.FromRgb(99, 102, 241));     // Indigo
        private static readonly SolidColorBrush ProjectBrush = new(Color.FromRgb(16, 185, 129)); // Green
        private static readonly SolidColorBrush TaskBrush = new(Color.FromRgb(245, 158, 11));    // Amber

        #endregion

        #region Dependency Properties

        /// <summary>
        /// The IMeasurable object to display. If set, auto-populates other properties.
        /// </summary>
        public static readonly DependencyProperty MeasurableProperty =
            DependencyProperty.Register(nameof(Measurable), typeof(IMeasurable), typeof(MeasurableItem),
                new PropertyMetadata(null, OnMeasurableChanged));

        /// <summary>
        /// The measurable type (for icon/color when not using IMeasurable).
        /// </summary>
        public static readonly DependencyProperty MeasurableTypeProperty =
            DependencyProperty.Register(nameof(MeasurableType), typeof(MeasurableType?), typeof(MeasurableItem),
                new PropertyMetadata(null, OnTypeChanged));

        /// <summary>
        /// The display name.
        /// </summary>
        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register(nameof(DisplayName), typeof(string), typeof(MeasurableItem),
                new PropertyMetadata("Measurable"));

        /// <summary>
        /// The display value (e.g., "75%" or "3/4 tasks").
        /// </summary>
        public static readonly DependencyProperty DisplayValueProperty =
            DependencyProperty.Register(nameof(DisplayValue), typeof(string), typeof(MeasurableItem),
                new PropertyMetadata(""));

        /// <summary>
        /// Whether the remove button is visible.
        /// </summary>
        public static readonly DependencyProperty CanRemoveProperty =
            DependencyProperty.Register(nameof(CanRemove), typeof(bool), typeof(MeasurableItem),
                new PropertyMetadata(true));

        /// <summary>
        /// The type label text.
        /// </summary>
        public static readonly DependencyProperty TypeLabelProperty =
            DependencyProperty.Register(nameof(TypeLabel), typeof(string), typeof(MeasurableItem),
                new PropertyMetadata(""));

        /// <summary>
        /// The icon path for the type.
        /// </summary>
        public static readonly DependencyProperty TypeIconPathProperty =
            DependencyProperty.Register(nameof(TypeIconPath), typeof(string), typeof(MeasurableItem),
                new PropertyMetadata(KpiIconPath));

        /// <summary>
        /// The icon background brush.
        /// </summary>
        public static readonly DependencyProperty TypeIconBackgroundProperty =
            DependencyProperty.Register(nameof(TypeIconBackground), typeof(Brush), typeof(MeasurableItem),
                new PropertyMetadata(KpiBrush));

        #endregion

        #region Routed Events

        /// <summary>
        /// Event raised when remove is clicked.
        /// </summary>
        public static readonly RoutedEvent RemoveClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(RemoveClicked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(MeasurableItem));

        #endregion

        #region Properties

        public IMeasurable? Measurable
        {
            get => (IMeasurable?)GetValue(MeasurableProperty);
            set => SetValue(MeasurableProperty, value);
        }

        public MeasurableType? MeasurableType
        {
            get => (MeasurableType?)GetValue(MeasurableTypeProperty);
            set => SetValue(MeasurableTypeProperty, value);
        }

        public string DisplayName
        {
            get => (string)GetValue(DisplayNameProperty);
            set => SetValue(DisplayNameProperty, value);
        }

        public string DisplayValue
        {
            get => (string)GetValue(DisplayValueProperty);
            set => SetValue(DisplayValueProperty, value);
        }

        public bool CanRemove
        {
            get => (bool)GetValue(CanRemoveProperty);
            set => SetValue(CanRemoveProperty, value);
        }

        public string TypeLabel
        {
            get => (string)GetValue(TypeLabelProperty);
            set => SetValue(TypeLabelProperty, value);
        }

        public string TypeIconPath
        {
            get => (string)GetValue(TypeIconPathProperty);
            set => SetValue(TypeIconPathProperty, value);
        }

        public Brush TypeIconBackground
        {
            get => (Brush)GetValue(TypeIconBackgroundProperty);
            set => SetValue(TypeIconBackgroundProperty, value);
        }

        #endregion

        #region Events

        public event RoutedEventHandler RemoveClicked
        {
            add => AddHandler(RemoveClickedEvent, value);
            remove => RemoveHandler(RemoveClickedEvent, value);
        }

        #endregion

        #region Constructor

        public MeasurableItem()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private static void OnMeasurableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MeasurableItem item && e.NewValue is IMeasurable measurable)
            {
                item.DisplayName = measurable.DisplayName;
                
                // Get DisplayValue and MeasurableType from concrete types
                switch (measurable)
                {
                    case KeyPerformanceIndicator kpi:
                        item.DisplayValue = kpi.DisplayValue;
                        item.MeasurableType = Interfaces.MeasurableType.Metric;
                        break;
                    case Project project:
                        item.DisplayValue = project.DisplayValue;
                        item.MeasurableType = Interfaces.MeasurableType.Project;
                        break;
                    case TaskCollection tc:
                        item.DisplayValue = tc.DisplayValue;
                        item.MeasurableType = Interfaces.MeasurableType.TaskCollection;
                        break;
                    default:
                        item.DisplayValue = string.Empty;
                        item.MeasurableType = Interfaces.MeasurableType.Metric;
                        break;
                }
            }
        }

        private static void OnTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MeasurableItem item)
            {
                item.UpdateTypeVisuals();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(RemoveClickedEvent, this));
            e.Handled = true;
        }

        #endregion

        #region Private Methods

        private void UpdateTypeVisuals()
        {
            switch (MeasurableType)
            {
                case Interfaces.MeasurableType.Metric:
                    TypeLabel = "KPI";
                    TypeIconPath = KpiIconPath;
                    TypeIconBackground = KpiBrush;
                    break;

                case Interfaces.MeasurableType.Project:
                    TypeLabel = "Project";
                    TypeIconPath = ProjectIconPath;
                    TypeIconBackground = ProjectBrush;
                    break;

                case Interfaces.MeasurableType.TaskCollection:
                    TypeLabel = "Task Collection";
                    TypeIconPath = TaskCollectionIconPath;
                    TypeIconBackground = TaskBrush;
                    break;

                default:
                    TypeLabel = "Unknown";
                    TypeIconPath = KpiIconPath;
                    TypeIconBackground = KpiBrush;
                    break;
            }
        }

        #endregion
    }
}

