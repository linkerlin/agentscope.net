using System;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AgentScope.Client.Services;
using AgentScope.Client.ViewModels;
using AgentScope.Client.Views;

namespace AgentScope.Client;

internal sealed class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        // 自动创建/迁移 SQLite 数据库和表
        InitDatabase(provider);

        App.ServiceProvider = provider;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void InitDatabase(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        // 第一轮：尝试 EnsureCreated（数据库不存在时直接建表）
        bool created;
        using (var db = factory.CreateDbContext())
        {
            created = db.Database.EnsureCreated();
        }

        if (created) return; // 全新数据库，表已齐全

        // 第二轮：数据库已存在，用原始 SQL 探测列名是否匹配最新模型
        bool schemaOk;
        try
        {
            using var db = factory.CreateDbContext();
            // 同时探测新表（McpConfigs）和新列（AgentConfigs.McpId）
            db.Database.ExecuteSqlRaw("SELECT \"McpId\" FROM \"AgentConfigs\" LIMIT 1");
            schemaOk = true;
        }
        catch
        {
            schemaOk = false;
        }

        if (!schemaOk)
        {
            // 架构变更 → 删除旧库重建
            using var db = factory.CreateDbContext();
            db.Database.EnsureDeleted();

            using var db2 = factory.CreateDbContext();
            db2.Database.EnsureCreated();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            var dbPath = System.IO.Path.Combine(
                AppContext.BaseDirectory, "agentscope_client.db");
            options.UseSqlite($"Data Source={dbPath}");
        });

        services.AddSingleton<ISessionStore, SessionStore>();
        services.AddSingleton<IProviderFactory, ProviderFactory>();
        services.AddSingleton<AgentConfigService>();
        services.AddSingleton<ChatService>();
        services.AddSingleton<McpConfigService>();
        services.AddSingleton<SkillConfigService>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AgentListViewModel>();
        services.AddTransient<McpListViewModel>();
        services.AddTransient<SkillListViewModel>();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
