using Avalonia.Controls;
using DecompilerGUI.ViewModels;

namespace DecompilerGUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
