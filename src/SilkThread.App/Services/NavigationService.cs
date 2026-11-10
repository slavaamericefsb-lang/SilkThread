using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Views;

namespace SilkThread.App.Services;

public interface INavigationService
{
    void Initialize(Frame frame);
    bool Navigate(Type pageType, object? parameter = null);
    bool Navigate(string route, object? parameter = null);
    void RegisterRoute(string route, Type pageType);
    bool GoBack();
    bool CanGoBack { get; }
}

public class NavigationService : INavigationService
{
    private Frame? _frame;
    private readonly Dictionary<string, Type> _routes = new(StringComparer.OrdinalIgnoreCase);

    public NavigationService()
    {
        // Default app route registrations
        RegisterRoute("Dashboard", typeof(DashboardPage));
        RegisterRoute("Tweaks", typeof(TweaksPage));
        RegisterRoute("Scanner", typeof(PcScannerPage));
        RegisterRoute("Optimization", typeof(OptimizationPage));
        RegisterRoute("Games", typeof(GamesPage));
        RegisterRoute("History", typeof(HistoryPage));
        RegisterRoute("Restore", typeof(RestoreBackupPage));
        RegisterRoute("Settings", typeof(SettingsPage));
    }

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void RegisterRoute(string route, Type pageType)
    {
        _routes[route] = pageType;
    }

    public bool Navigate(string route, object? parameter = null)
    {
        if (_routes.TryGetValue(route, out var pageType))
        {
            return Navigate(pageType, parameter);
        }
        return false;
    }

    public bool Navigate(Type pageType, object? parameter = null)
    {
        if (_frame == null) return false;
        _ = PageTransitionHelper.TransitionNavigateAsync(_frame, pageType, parameter);
        return true;
    }

    public bool GoBack()
    {
        if (CanGoBack)
        {
            _frame?.GoBack();
            return true;
        }
        return false;
    }
}
