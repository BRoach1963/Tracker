using System.Windows.Controls;
using System.Windows;

namespace Tracker.Controls
{
    public class TrackerToolBar : ToolBar
    {
        private Button? _overflowButton;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var size = base.MeasureOverride(constraint);
            return size;
        }
    }
}
