// Global using directives to resolve WPF vs WinForms namespace conflicts
// When UseWindowsForms is enabled alongside UseWPF, many types become ambiguous.
// These aliases prefer WPF types for the application.

// Core types
global using Application = System.Windows.Application;
global using Timer = System.Threading.Timer;
global using DialogResult = Tracker.Classes.DialogResult;

// Controls
global using UserControl = System.Windows.Controls.UserControl;
global using Button = System.Windows.Controls.Button;
global using TextBox = System.Windows.Controls.TextBox;
global using CheckBox = System.Windows.Controls.CheckBox;
global using ComboBox = System.Windows.Controls.ComboBox;
global using ListBox = System.Windows.Controls.ListBox;
global using Label = System.Windows.Controls.Label;
global using Panel = System.Windows.Controls.Panel;
global using Control = System.Windows.Controls.Control;
global using TabControl = System.Windows.Controls.TabControl;
global using ContextMenu = System.Windows.Controls.ContextMenu;
global using MenuItem = System.Windows.Controls.MenuItem;
global using ToolTip = System.Windows.Controls.ToolTip;
global using Image = System.Windows.Controls.Image;
global using ProgressBar = System.Windows.Controls.ProgressBar;
global using GroupBox = System.Windows.Controls.GroupBox;
global using TreeView = System.Windows.Controls.TreeView;
global using ListView = System.Windows.Controls.ListView;
global using DataGrid = System.Windows.Controls.DataGrid;
global using RichTextBox = System.Windows.Controls.RichTextBox;
global using ScrollBar = System.Windows.Controls.Primitives.ScrollBar;

// Input
global using Cursor = System.Windows.Input.Cursor;
global using Cursors = System.Windows.Input.Cursors;
global using KeyEventArgs = System.Windows.Input.KeyEventArgs;
global using KeyEventHandler = System.Windows.Input.KeyEventHandler;
global using MouseEventArgs = System.Windows.Input.MouseEventArgs;
global using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
global using DragEventArgs = System.Windows.DragEventArgs;

// Data binding
global using Binding = System.Windows.Data.Binding;

// Graphics
global using Point = System.Windows.Point;
global using Size = System.Windows.Size;
global using Brush = System.Windows.Media.Brush;
global using Brushes = System.Windows.Media.Brushes;
global using Color = System.Windows.Media.Color;
global using FontFamily = System.Windows.Media.FontFamily;

// Dialogs (prefer Microsoft.Win32 over WinForms)
global using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
global using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

// Other
global using Clipboard = System.Windows.Clipboard;
global using MessageBox = System.Windows.MessageBox;
global using DataFormats = System.Windows.DataFormats;
global using HorizontalAlignment = System.Windows.HorizontalAlignment;
global using VerticalAlignment = System.Windows.VerticalAlignment;

