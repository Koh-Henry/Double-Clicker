using Avalonia.Controls;
using MinecraftDoubleClicker.ViewModels;

namespace MinecraftDoubleClicker;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel)
        : this()
    {
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
