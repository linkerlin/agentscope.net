using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using AgentScope.Client.ViewModels;

namespace AgentScope.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<MainWindowViewModel>();
        Loaded += async (_, _) =>
        {
            var vm = (MainWindowViewModel)DataContext!;
            await vm.LoadSessionsCommand.ExecuteAsync(null);
        };
    }
}
