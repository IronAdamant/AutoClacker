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
            if (Properties.Settings.Default.TriggerKey != 116) // 116 is Key.F5
            {
                Properties.Settings.Default.TriggerKey = 116;
                Properties.Settings.Default.Save();
            }
            viewModel = new MainViewModel(this);
            DataContext = viewModel;
            KeyDown += MainWindow_KeyDown;
            Loaded += MainWindow_Loaded;
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
    }
}