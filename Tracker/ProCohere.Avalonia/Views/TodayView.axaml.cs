using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views;

public partial class TodayView : UserControl
{
    public TodayView()
    {
        InitializeComponent();
        DataContext = new TodayViewModel();
    }
}
