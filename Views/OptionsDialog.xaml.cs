using System.Windows;
using System.Windows.Input;
using AutoClacker.ViewModels;

namespace AutoClacker.Views
{
    public partial class OptionsDialog : Window
    {
        private readonly string originalTheme;
        private readonly MainViewModel viewModel;

        public OptionsDialog(MainViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            DataContext = viewModel;
            originalTheme = viewModel.Theme;
            LoadTheme(viewModel.Theme);
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Theme = originalTheme; // Revert to original theme
            }
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Theme = originalTheme; // Revert to original theme
            }
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}