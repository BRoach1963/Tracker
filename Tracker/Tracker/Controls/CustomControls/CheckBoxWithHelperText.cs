using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Tracker.Controls.CustomControls
{
    public class CheckBoxWithHelperText : CheckBox
    {
        static CheckBoxWithHelperText()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CheckBoxWithHelperText),
                new FrameworkPropertyMetadata(typeof(CheckBoxWithHelperText)));
        }

        public static readonly DependencyProperty HelperTextProperty =
            DependencyProperty.Register(nameof(HelperText), typeof(string),
                typeof(CheckBoxWithHelperText), new PropertyMetadata(string.Empty));

        public string HelperText
        {
            get => (string)GetValue(HelperTextProperty);
            set => SetValue(HelperTextProperty, value);
        }
    }
}
