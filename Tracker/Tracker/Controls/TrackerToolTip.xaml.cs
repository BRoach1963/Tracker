using System.Windows;
using System.Windows.Controls;

namespace Tracker.Controls
{
    /// <summary>
    /// Interaction logic for TrackerToolTip.xaml
    /// </summary>
    public partial class TrackerToolTip : UserControl
    {
        public TrackerToolTip()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ToolTipTextProperty =
            DependencyProperty.Register(nameof(ToolTipText), typeof(string),
                typeof(TrackerToolTip), new PropertyMetadata(string.Empty));

        public string ToolTipText
        {
            get => (string)GetValue(ToolTipTextProperty);
            set => SetValue(ToolTipTextProperty, value);
        }
    }
}
