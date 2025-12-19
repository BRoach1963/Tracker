using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// A chat-style Help Bot control powered by AI.
    /// </summary>
    public partial class HelpBotControl : UserControl
    {
        private HelpBotViewModel? _viewModel;

        public HelpBotControl()
        {
            InitializeComponent();
            
            _viewModel = new HelpBotViewModel();
            DataContext = _viewModel;

            // Auto-scroll when new messages are added
            if (_viewModel?.Messages != null)
            {
                _viewModel.Messages.CollectionChanged += Messages_CollectionChanged;
            }

            Loaded += HelpBotControl_Loaded;
            Unloaded += HelpBotControl_Unloaded;
        }

        private void HelpBotControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus the input box
            InputTextBox?.Focus();
        }

        private void HelpBotControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Clean up
            if (_viewModel?.Messages != null)
            {
                _viewModel.Messages.CollectionChanged -= Messages_CollectionChanged;
            }
            _viewModel?.Dispose();
        }

        private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Auto-scroll to bottom when new messages are added
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessagesScrollViewer?.ScrollToEnd();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Send on Enter (without Shift)
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (_viewModel?.SendCommand.CanExecute(null) == true)
                {
                    _viewModel.SendCommand.Execute(null);
                }
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Template selector for chat messages (user vs assistant).
    /// </summary>
    public class ChatMessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? UserTemplate { get; set; }
        public DataTemplate? AssistantTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is ChatMessageViewModel message)
            {
                return message.IsUser ? UserTemplate : AssistantTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}

