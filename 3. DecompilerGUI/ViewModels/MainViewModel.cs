using CommunityToolkit.Mvvm.ComponentModel;

namespace DecompilerGUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string greeting = "Welcome to Avalonia!";
}
