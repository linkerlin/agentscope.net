using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AgentScope.Client.ViewModels;

namespace AgentScope.Client.Views;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null) return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View")
            .Replace("ViewModels", "Views");
        var type = System.Type.GetType(name);

        if (type != null)
        {
            return (Control)System.Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = $"View not found: {name}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
