using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Tracker.ViewModels;

namespace Tracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new TrackerMainViewModel();
            SubscribeToSizingEvents();
            this.Unloaded += (_, _) =>
            { 
                UnsubscribeFromSizingEvents();
                this.Unloaded -= (RoutedEventHandler)((_, _) => { /* Same logic here if needed */ });
            };
        }

        private void SubscribeToSizingEvents()
        {
            this.TeamControl.SizeChanged += Control_SizeChanged;
            this.OneOnOnesControl.SizeChanged += Control_SizeChanged;
            this.KpisControl.SizeChanged += Control_SizeChanged;
            this.OkrsControl.SizeChanged += Control_SizeChanged;
            this.TasksControl.SizeChanged += Control_SizeChanged;
            this.ProjectsControl.SizeChanged += Control_SizeChanged;
        }

        private void UnsubscribeFromSizingEvents()
        {
            this.TeamControl.SizeChanged -= Control_SizeChanged;
            this.OneOnOnesControl.SizeChanged -= Control_SizeChanged;
            this.KpisControl.SizeChanged -= Control_SizeChanged;
            this.OkrsControl.SizeChanged -= Control_SizeChanged;
            this.TasksControl.SizeChanged -= Control_SizeChanged;
            this.ProjectsControl.SizeChanged -= Control_SizeChanged;
        }

        private void Control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is FrameworkElement control)
            {
                if (control.RenderTransform is TranslateTransform transform)
                {
                    // If the control is not visible (i.e., not selected), keep it off-screen
                    // Otherwise, ensure it's in the correct position
                    var tabItem = FindParentTabItem(control);
                    {
                        bool isSelected = tabItem is { IsSelected: true };
                        transform.X = isSelected ? 0 : control.ActualWidth;
                    }
                }
            }
        }

        private TabItem? FindParentTabItem(DependencyObject element)
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(element);
            while (parent != null && !(parent is TabItem))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as TabItem ?? null;
        }
 
        private void TabChangedEventHandler(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedTab = e.AddedItems[0] as TabItem;
                if (selectedTab != null)
                {
                    // Identify the corresponding control based on the selected tab
                    FrameworkElement? correspondingControl = null;

                    switch (selectedTab.Name)
                    {
                        case "Team":
                            correspondingControl = TeamControl;
                            break;
                        case "OneOnOne":
                            correspondingControl = OneOnOnesControl;
                            break;
                        case "Projects":
                            correspondingControl = ProjectsControl;
                            break;
                        case "Tasks":
                            correspondingControl = TasksControl;
                            break;
                        case "Kpis":
                            correspondingControl = this.KpisControl;
                            break;
                        case "Okrs":
                            correspondingControl = OkrsControl;
                            break;

                    }

                    if (correspondingControl != null)
                    {
                        if (!(correspondingControl.RenderTransform is TranslateTransform))
                        {
                            correspondingControl.RenderTransform = new TranslateTransform();
                        }

                        var transform = (TranslateTransform)correspondingControl.RenderTransform;
                        transform.X = correspondingControl.ActualWidth; // Set the starting position

                        var slideInAnimation = new DoubleAnimation
                        {
                            From = correspondingControl.ActualWidth,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.4),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };

                        transform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
                    }
                }
            }
        }
    }
}