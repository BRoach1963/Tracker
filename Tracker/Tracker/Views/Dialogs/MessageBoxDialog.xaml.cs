using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tracker.Controls;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Custom styled message box dialog to replace Windows MessageBox.
    /// </summary>
    public partial class MessageBoxDialog : BaseWindow
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public MessageBoxDialog(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();
            
            Title = title;
            MessageText.Text = message;
            TitleText.Text = title;
            
            // Set icon based on MessageBoxImage
            SetIcon(icon);
            
            // Create buttons based on MessageBoxButton
            CreateButtons(button);
        }

        private void SetIcon(MessageBoxImage icon)
        {
            Geometry iconData;
            Brush iconBrush;

            // Handle Exclamation (same value as Warning)
            if (icon == MessageBoxImage.Exclamation)
            {
                icon = MessageBoxImage.Warning;
            }

            switch (icon)
            {
                case MessageBoxImage.Error:
                    iconData = (Geometry)FindResource("ErrorIcon");
                    iconBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Error red
                    break;
                case MessageBoxImage.Warning:
                    iconData = (Geometry)FindResource("WarningIcon");
                    iconBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Warning orange
                    break;
                case MessageBoxImage.Question:
                    iconData = (Geometry)FindResource("InfoIcon");
                    iconBrush = (Brush)FindResource("AccentBrush");
                    break;
                case MessageBoxImage.Information:
                default:
                    iconData = (Geometry)FindResource("InfoIcon");
                    iconBrush = (Brush)FindResource("AccentBrush");
                    break;
            }

            MessageIcon.Data = iconData;
            MessageIcon.Fill = iconBrush;
        }

        private void CreateButtons(MessageBoxButton button)
        {
            ButtonPanel.Children.Clear();

            switch (button)
            {
                case MessageBoxButton.OK:
                    AddButton("OK", MessageBoxResult.OK, isDefault: true);
                    break;
                case MessageBoxButton.OKCancel:
                    AddButton("Cancel", MessageBoxResult.Cancel);
                    AddButton("OK", MessageBoxResult.OK, isDefault: true);
                    break;
                case MessageBoxButton.YesNo:
                    AddButton("No", MessageBoxResult.No);
                    AddButton("Yes", MessageBoxResult.Yes, isDefault: true);
                    break;
                case MessageBoxButton.YesNoCancel:
                    AddButton("Cancel", MessageBoxResult.Cancel);
                    AddButton("No", MessageBoxResult.No);
                    AddButton("Yes", MessageBoxResult.Yes, isDefault: true);
                    break;
            }
        }

        private void AddButton(string content, MessageBoxResult result, bool isDefault = false)
        {
            var btn = new Button
            {
                Content = content,
                Style = (Style)FindResource("DialogButtonStyle"),
                Margin = new Thickness(5, 0, 0, 0),
                Padding = new Thickness(20, 8, 20, 8),
                MinWidth = 80
            };

            if (isDefault)
            {
                btn.IsDefault = true;
            }

            btn.Click += (s, e) =>
            {
                Result = result;
                DialogResult = result == MessageBoxResult.Cancel || result == MessageBoxResult.No 
                    ? false 
                    : true;
                Close();
            };

            ButtonPanel.Children.Add(btn);
        }
    }
}

