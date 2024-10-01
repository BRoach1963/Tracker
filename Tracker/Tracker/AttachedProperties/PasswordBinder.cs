using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace Tracker.AttachedProperties
{
    public class PasswordBinder
    {
        public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBinder),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BindPassword",
                typeof(bool),
                typeof(PasswordBinder),
                new PropertyMetadata(false, OnBindPasswordChanged));

        private static readonly DependencyProperty UpdatingPasswordProperty =
            DependencyProperty.RegisterAttached(
                "UpdatingPassword",
                typeof(bool),
                typeof(PasswordBinder),
                new PropertyMetadata(false));

        public static string GetBoundPassword(DependencyObject dp)
        {
            return (string)dp.GetValue(BoundPasswordProperty);
        }

        public static void SetBoundPassword(DependencyObject dp, string value)
        {
            dp.SetValue(BoundPasswordProperty, value);
        }

        public static bool GetBindPassword(DependencyObject dp)
        {
            return (bool)dp.GetValue(BindPasswordProperty);
        }

        public static void SetBindPassword(DependencyObject dp, bool value)
        {
            dp.SetValue(BindPasswordProperty, value);
        }

        private static bool GetUpdatingPassword(DependencyObject dp)
        {
            return (bool)dp.GetValue(UpdatingPasswordProperty);
        }

        private static void SetUpdatingPassword(DependencyObject dp, bool value)
        {
            dp.SetValue(UpdatingPasswordProperty, value);
        }

        private static void OnBoundPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            if (dp is PasswordBox passwordBox && !(bool)GetUpdatingPassword(dp))
            {
                passwordBox.Password = (string)e.NewValue;
            }
        }

        private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            if (dp is PasswordBox passwordBox)
            {
                if ((bool)e.NewValue)
                {
                    passwordBox.PasswordChanged += PasswordChanged;
                }
                else
                {
                    passwordBox.PasswordChanged -= PasswordChanged;
                }
            }
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                SetUpdatingPassword(passwordBox, true);
                SetBoundPassword(passwordBox, passwordBox.Password);
                SetUpdatingPassword(passwordBox, false);
            }
        }
    }
}
