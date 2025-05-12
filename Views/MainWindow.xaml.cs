using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoClacker.ViewModels;

namespace AutoClacker.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainViewModel(this);
            DataContext = viewModel;
            KeyDown += MainWindow_KeyDown;
            Loaded += MainWindow_Loaded;
            viewModel.CurrentSettings.PropertyChanged += Settings_PropertyChanged;
            LoadTheme(viewModel.Theme);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.InitializeHotkeyManager(this);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            viewModel.OnKeyDown(e);
            if (!(e.OriginalSource is TextBox))
            {
                e.Handled = true;
            }
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(viewModel.CurrentSettings.Theme))
            {
                LoadTheme(viewModel.Theme);
            }
        }

        private void LoadTheme(string theme)
        {
            Resources.MergedDictionaries.Clear();
            var themeDict = new ResourceDictionary
            {
                Source = new System.Uri($"/Themes/{theme}Theme.xaml", System.UriKind.Relative)
            };
            Resources.MergedDictionaries.Add(themeDict);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            var button = sender as Button;
            if (button != null)
            {
                button.Content = WindowState == WindowState.Maximized ? "🗗" : "□";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}