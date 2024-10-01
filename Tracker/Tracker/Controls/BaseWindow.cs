using System.Windows;
using System.Windows.Input;
using Tracker.Common.Enums;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Interfaces;
using Tracker.Managers;

namespace Tracker.Controls
{

    public class BaseWindow : Window, ICloseable
    {
        private const UInt32 SwpNoSize = 0x0001;
        private const UInt32 SwpNoMove = 0x0002;

        private bool _resizing = false;
        private Point _startPoint;
        private double _originalWidth;
        private double _originalHeight;
        private double _originalLeft;
        private double _originalTop;
        private ResizeDirection _resizeDirection;

        public BaseWindow(DialogType type = DialogType.MainWindow, double minHeight = 400, double minWidth = 400)
        {
            Type = type;
            MinWidth = minWidth;
            MinHeight = minHeight;

            MouseMove += OnMouseMove;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            Messenger.Subscribe<DialogCloseMessage>(HandleDialogCloseMessage);
        }

        private void HandleDialogCloseMessage(DialogCloseMessage msg)
        {
            if (msg.DialogType == Type)
            {
                if (DataContext is IDisposable vm)
                {
                    vm.Dispose();
                }
                Messenger.Unsubscribe<DialogCloseMessage>(HandleDialogCloseMessage);
                DialogManager.Instance.CloseDialog(this);
            }
        }

        public DialogType Type { get; set; }

        protected virtual void OnDragHandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Ensure this only triggers drag move when the window is not resizing
            if (_resizeDirection == ResizeDirection.None && e.ChangedButton == MouseButton.Left)
            {
                e.Handled = true;
                DragMove();
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            _resizeDirection = GetResizeDirection(_startPoint);

            if (_resizeDirection != ResizeDirection.None)
            {
                // Initiate resizing
                _resizing = true;
                CaptureMouse();

                _originalWidth = Width;
                _originalHeight = Height;
                _originalLeft = Left;
                _originalTop = Top;
            }
            else if (e.LeftButton == MouseButtonState.Pressed && _resizeDirection == ResizeDirection.None)
            {
                // If not resizing, initiate window drag if click is not near the edges
                OnDragHandleMouseDown(sender, e);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_resizing)
            {
                Point currentPoint = e.GetPosition(this);
                ResizeWindow(currentPoint);
            }
            else
            {
                var cursor = GetCursorForPosition(e.GetPosition(this));
                Cursor = cursor;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _resizing = false;
            ReleaseMouseCapture();
        }

        private ResizeDirection GetResizeDirection(Point point)
        {
            const int edgeThreshold = 10;

            if (point.X <= edgeThreshold) return ResizeDirection.Left;
            if (point.X >= ActualWidth - edgeThreshold) return ResizeDirection.Right;
            //if (point.Y <= edgeThreshold) return ResizeDirection.Top;
            if (point.Y >= ActualHeight - edgeThreshold) return ResizeDirection.Bottom;

            return ResizeDirection.None;
        }

        private void ResizeWindow(Point currentPoint)
        {
            double deltaX = currentPoint.X - _startPoint.X;
            double deltaY = currentPoint.Y - _startPoint.Y;

            switch (_resizeDirection)
            {
                case ResizeDirection.Left:
                    if (_originalWidth - deltaX >= MinWidth)
                    {
                        Width = _originalWidth - deltaX;
                        Left = _originalLeft + deltaX;
                    }
                    break;
                case ResizeDirection.Right:
                    if (_originalWidth + deltaX >= MinWidth)
                    {
                        Width = _originalWidth + deltaX;
                    }
                    break;
                //case ResizeDirection.Top:
                //    if (_originalHeight - deltaY >= MinHeight)
                //    {
                //        Height = _originalHeight - deltaY;
                //        Top = _originalTop + deltaY;
                //    }
                //    break;
                case ResizeDirection.Bottom:
                    if (_originalHeight + deltaY >= MinHeight)
                    {
                        Height = _originalHeight + deltaY;
                    }
                    break;
            }
        }

        private Cursor GetCursorForPosition(Point point)
        {
            const int edgeThreshold = 10;

            // Ignore top edge for angled cursors and up/down resizing 
            if (point.X <= edgeThreshold) return Cursors.SizeWE;  // Left edge
            if (point.X >= ActualWidth - edgeThreshold) return Cursors.SizeWE;  // Right edge
            if (point.Y >= ActualHeight - edgeThreshold) return Cursors.SizeNS;  // Bottom edge

            return Cursors.Arrow;  // Default cursor for other areas, including the top edge 
        }

        private enum ResizeDirection
        {
            None,
            Left,
            Right,
            //Top,
            Bottom,
        }


        protected void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogManager.Instance.CloseDialog(this);
        }

        protected void Maximize_Click(object sender, RoutedEventArgs e)
        {
            var titleBar = (DialogTitleBar)FindName("TitleBar");

            if (titleBar == null)
            {
                return;
            }
            WindowState = WindowState.Maximized;
            if (titleBar.Maximize != null) titleBar.Maximize.Visibility = Visibility.Collapsed;
            if (titleBar.Restore != null) titleBar.Restore.Visibility = Visibility.Visible;
        }

        protected void Restore_Click(object sender, RoutedEventArgs e)
        {
            var titleBar = (DialogTitleBar)FindName("TitleBar");

            if (titleBar == null)
            {
                return;
            }
            WindowState = WindowState.Normal;
            if (titleBar.Maximize != null) titleBar.Maximize.Visibility = Visibility.Visible;
            if (titleBar.Restore != null) titleBar.Restore.Visibility = Visibility.Collapsed;
        }

        protected void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
