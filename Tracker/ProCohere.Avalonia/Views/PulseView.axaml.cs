using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views;

public partial class PulseView : UserControl
{
    public PulseView()
    {
        InitializeComponent();
        DataContext = new PulseViewModel();
    }
}
