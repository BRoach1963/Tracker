using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views.Briefing;

public partial class BriefingView : UserControl
{
    public BriefingView()
    {
        InitializeComponent();
        DataContext = new BriefingViewModel();
    }
}
