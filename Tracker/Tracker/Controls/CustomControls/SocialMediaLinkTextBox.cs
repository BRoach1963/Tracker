using DeepEndControls.Enums;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using Tracker.Command;

namespace Tracker.Controls.CustomControls
{
    public class SocialMediaLinkTextBox : TextBox
    {
        #region Commands

        public ICommand LaunchUrlCommand { get; private set; }

        private bool CanExecuteUrlCommand(object? obj)
        {
            return !string.IsNullOrEmpty(this.Text);
        }

        private void ExecuteLaunchUrlCommand(object? obj)
        {
            if (string.IsNullOrEmpty(this.Text)) return;

            Process.Start(FormatUrl(this.Text));
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty HintTextProperty =
            DependencyProperty.Register(nameof(HintText), typeof(string), typeof(SocialMediaLinkTextBox), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SocialMediaTypeProperty =
            DependencyProperty.Register(nameof(SocialMediaType), typeof(SocialMediaEnum), typeof(SocialMediaLinkTextBox),
                new PropertyMetadata(SocialMediaEnum.None, OnSocialMediaTypeChanged));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(SocialMediaLinkTextBox), new PropertyMetadata(Geometry.Empty));

        public string HintText
        {
            get => (string)GetValue(HintTextProperty);
            set => SetValue(HintTextProperty, value);
        }

        public SocialMediaEnum SocialMediaType
        {
            get => (SocialMediaEnum)GetValue(SocialMediaTypeProperty);
            set => SetValue(SocialMediaTypeProperty, value);
        }

        public Geometry Icon
        {
            get => (Geometry)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        #endregion

        #region Ctor
        static SocialMediaLinkTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SocialMediaLinkTextBox), new FrameworkPropertyMetadata(typeof(SocialMediaLinkTextBox)));
        }

        public SocialMediaLinkTextBox()
        {
            SubscribeToControlEvents();
            LaunchUrlCommand = new TrackerCommand(ExecuteLaunchUrlCommand, CanExecuteUrlCommand);
        }

        #endregion

        #region Private Methods

        private void SubscribeToControlEvents()
        {
            this.Unloaded += OnUnloaded;
        }

        private void UnsubscribeToControlEvents()
        {
            this.Unloaded -= OnUnloaded;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            //if (string.IsNullOrWhiteSpace(this.Text))
            //{
            //    UpdateHintVisibility();
            //}
        }

        private void UpdateHintVisibility()
        {
            //if (GetTemplateChild("HintTextBox") is TextBoxWithHint hintTextBlock)
            //{
            //    if (string.IsNullOrWhiteSpace(this.Text))
            //    {
            //        this.HintText = HintText;
            //        hintTextBlock.Visibility = Visibility.Visible;
            //    }
            //    else
            //    {
            //        this.Foreground = Brushes.Black;
            //        hintTextBlock.Visibility = Visibility.Collapsed;
            //    }
            //}
        }
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateHintVisibility();
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            UpdateHintVisibility();
        }

        private void SetIcon()
        {
            switch (SocialMediaType)
            {
                case SocialMediaEnum.Facebook:
                    Icon = Geometry.Parse("M5,3H19A2,2 0 0,1 21,5V19A2,2 0 0,1 19,21H5A2,2 0 0,1 3,19V5A2,2 0 0,1 5,3M18,5H15.5A3.5,3.5 0 0,0 12,8.5V11H10V14H12V21H15V14H18V11H15V9A1,1 0 0,1 16,8H18V5Z");
                    break;
                case SocialMediaEnum.LinkedIn:
                    Icon = Geometry.Parse("M19 3A2 2 0 0 1 21 5V19A2 2 0 0 1 19 21H5A2 2 0 0 1 3 19V5A2 2 0 0 1 5 3H19M18.5 18.5V13.2A3.26 3.26 0 0 0 15.24 9.94C14.39 9.94 13.4 10.46 12.92 11.24V10.13H10.13V18.5H12.92V13.57C12.92 12.8 13.54 12.17 14.31 12.17A1.4 1.4 0 0 1 15.71 13.57V18.5H18.5M6.88 8.56A1.68 1.68 0 0 0 8.56 6.88C8.56 5.95 7.81 5.19 6.88 5.19A1.69 1.69 0 0 0 5.19 6.88C5.19 7.81 5.95 8.56 6.88 8.56M8.27 18.5V10.13H5.5V18.5H8.27Z");
                    break;
                case SocialMediaEnum.Instagram:
                    Icon = Geometry.Parse("M7.8,2H16.2C19.4,2 22,4.6 22,7.8V16.2A5.8,5.8 0 0,1 16.2,22H7.8C4.6,22 2,19.4 2,16.2V7.8A5.8,5.8 0 0,1 7.8,2M7.6,4A3.6,3.6 0 0,0 4,7.6V16.4C4,18.39 5.61,20 7.6,20H16.4A3.6,3.6 0 0,0 20,16.4V7.6C20,5.61 18.39,4 16.4,4H7.6M17.25,5.5A1.25,1.25 0 0,1 18.5,6.75A1.25,1.25 0 0,1 17.25,8A1.25,1.25 0 0,1 16,6.75A1.25,1.25 0 0,1 17.25,5.5M12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9Z");
                    break;
                case SocialMediaEnum.XTwitter:
                    Icon = Geometry.Parse("M9,7H11L12,9.5L13,7H15L13,12L15,17H13L12,14.5L11,17H9L11,12L9,7M5,3H19A2,2 0 0,1 21,5V19A2,2 0 0,1 19,21H5A2,2 0 0,1 3,19V5A2,2 0 0,1 5,3M5,5V19H19V5H5");
                    break;
            }
        }

        #endregion

        #region Overrides

        protected void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            UnsubscribeToControlEvents();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateHintVisibility();
        }

        private ProcessStartInfo FormatUrl(string url)
        {
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            return new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
        }

        private static void OnSocialMediaTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SocialMediaLinkTextBox control)
            {
                control.SetIcon();
            }
        }

        #endregion
    }
}
