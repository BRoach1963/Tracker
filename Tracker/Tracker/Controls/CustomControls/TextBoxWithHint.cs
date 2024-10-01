using DeepEndControls.Controls.TextBox.ValidationRules;
using DeepEndControls.Enums;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;

namespace Tracker.Controls.CustomControls
{
    public class TextBoxWithHint : TextBox
    {
        #region Dependency Properties

        public static readonly DependencyProperty HintTextProperty =
            DependencyProperty.Register(nameof(HintText), typeof(string), typeof(TextBoxWithHint), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TextBoxTypeProperty =
            DependencyProperty.Register(nameof(TextBoxType), typeof(TextBoxType), typeof(TextBoxWithHint), new PropertyMetadata(TextBoxType.Text));

        //public new static readonly DependencyProperty TextProperty =
        //    DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextBoxWithHint),
        //        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChangedCallback)); 

        public string HintText
        {
            get => (string)GetValue(HintTextProperty);
            set => SetValue(HintTextProperty, value);
        }

        public TextBoxType TextBoxType
        {
            get => (TextBoxType)GetValue(TextBoxTypeProperty);
            set => SetValue(TextBoxTypeProperty, value);
        }

        #endregion

        #region Ctor
        static TextBoxWithHint()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBoxWithHint), new FrameworkPropertyMetadata(typeof(TextBoxWithHint)));
        }

        public TextBoxWithHint()
        {
            SubscribeToControlEvents();
        }

        #endregion

        #region Private Methods
        private static void OnTextChangedCallback(object sender, TextChangedEventArgs textChangedEventArgs)
        {
            var control = (TextBoxWithHint)sender;
            control.UpdateHintVisibility();
        }
 
        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            UpdateHintVisibility();
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            // Apply the hint text if the TextBox is empty
            if (string.IsNullOrWhiteSpace(this.Text))
            {
                ApplyHintText();
            }
            else
            {
                // Format the date according to the type
                if (TextBoxType == TextBoxType.LongDate || TextBoxType == TextBoxType.ShortDate)
                {
                    DateTime parsedDate;
                    string format = TextBoxType == TextBoxType.LongDate ? "MM/dd/yyyy" : "MM/dd";

                    if (DateTime.TryParse(this.Text, out parsedDate))
                    {
                        this.Text = parsedDate.ToString(format);
                    }
                    else
                    {
                        this.Text = "Invalid Date";
                        this.Foreground = Brushes.Red;
                    }
                }
            }

            UpdateHintVisibility();
        }

        private void ApplyHintText()
        {
            if (TextBoxType == TextBoxType.LongDate)
            {
                this.HintText = "MM/DD/YYYY";
            }
            else if (TextBoxType == TextBoxType.ShortDate)
            {
                this.HintText = "MM/DD";
            }
            else
            {
                this.HintText = HintText;
            }
        }

        private void UpdateHintVisibility()
        {
            if (GetTemplateChild("HintText") is TextBlock hintTextBlock)
            {
                if (string.IsNullOrWhiteSpace(this.Text))
                {
                    ApplyHintText();
                    hintTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    this.Foreground = Brushes.Black;
                    hintTextBlock.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SubscribeToControlEvents()
        {
            this.PreviewTextInput += OnPreviewTextInput; 
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            this.GotFocus += OnGotFocus;
            this.LostFocus += OnLostFocus;
            this.TextChanged += OnTextChangedCallback;
        }

        private void UnsubscribeToControlEvents()
        {
            this.PreviewTextInput -= OnPreviewTextInput; 
            this.Loaded -= OnLoaded;
            this.Unloaded -= OnUnloaded;
            this.GotFocus -= OnGotFocus;
            this.LostFocus -= OnLostFocus;
            this.TextChanged -= OnTextChangedCallback;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyValidationRules();
        }

        private void ApplyValidationRules()
        {
            // Retrieve the existing binding expression for the Text property
            var bindingExpression = BindingOperations.GetBindingExpression(this, TextProperty);

            // If there is an existing binding, modify it to include validation rules
            if (bindingExpression != null)
            {
                var binding = bindingExpression.ParentBinding;

                // Clear any previous validation rules
                binding.ValidationRules.Clear();

                // Add validation rules based on the TextBoxType
                switch (TextBoxType)
                {
                    case TextBoxType.Phone:
                        binding.ValidationRules.Add(new PhoneValidationRule());
                        break;
                    case TextBoxType.Email:
                        binding.ValidationRules.Add(new EmailValidationRule());
                        break;
                }

                // Apply the modified binding back to the Text property
                this.SetBinding(TextProperty, binding);
            }
            else
            {
                // If no binding exists, create and apply a new one
                Binding newBinding = new Binding("Text")
                {
                    Source = this,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    ValidatesOnDataErrors = true,
                    NotifyOnValidationError = true
                };

                // Add validation rules based on the TextBoxType
                switch (TextBoxType)
                {
                    case TextBoxType.Phone:
                        newBinding.ValidationRules.Add(new PhoneValidationRule());
                        break;
                    case TextBoxType.Email:
                        newBinding.ValidationRules.Add(new EmailValidationRule());
                        break; 
                }

                // Apply the new binding
                this.SetBinding(TextProperty, newBinding);
            }
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (TextBoxType == TextBoxType.Phone)
            {
                // Start with +1 if not already present
                if (!this.Text.StartsWith("+1"))
                {
                    this.Text = "+1 ";
                }

                // Concatenate new input
                string input = this.Text + e.Text;

                // Format the number
                this.Text = FormatPhoneNumber(input);
                e.Handled = true; // Prevent the default input
                this.CaretIndex = this.Text.Length; // Place the cursor at the end
            }
            else if (TextBoxType == TextBoxType.LongDate || TextBoxType == TextBoxType.ShortDate)
            {
                // Handle date input formatting
                string input = this.Text + e.Text;

                if (TextBoxType == TextBoxType.LongDate)
                {
                    this.Text = FormatDate(input, "MM/dd/yyyy");
                }
                else if (TextBoxType == TextBoxType.ShortDate)
                {
                    this.Text = FormatDate(input, "MM/dd");
                }

                e.Handled = true;
                this.CaretIndex = this.Text.Length;
            }
        }

        private string FormatPhoneNumber(string input)
        {
            // Remove any non-digit characters except the leading '+'
            string digits = new string(input.Where(c => char.IsDigit(c) || c == '+').ToArray());

            if (!digits.StartsWith("+1"))
            {
                digits = "+1 " + digits;
            }

            digits = digits.Replace("+1", "").Trim();

            // Format based on the number of digits
            if (digits.Length <= 3)
                return $"+1 ({digits}";
            else if (digits.Length <= 6)
                return $"+1 ({digits.Substring(0, 3)}) {digits.Substring(3)}";
            else if (digits.Length <= 10)
                return $"+1 ({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6)}";
            else
                return $"+1 ({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
        }

        private string FormatDate(string input, string format)
        {
            // Remove any non-digit characters except slashes
            string digits = new string(input.Where(c => char.IsDigit(c) || c == '/').ToArray());

            // Attempt to parse the date
            if (DateTime.TryParse(digits, out DateTime parsedDate))
            {
                return parsedDate.ToString(format);
            }

            // If parsing fails, return the raw digits (or potentially add custom handling)
            return digits;
        }

        #endregion

        #region Protected Methods

        protected void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            UnsubscribeToControlEvents();
        }

        #endregion

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateHintVisibility();
        }

        #endregion

    }
}
