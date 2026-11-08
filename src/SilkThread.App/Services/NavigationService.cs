using System;
using Microsoft.UI.Xaml.Controls;

namespace SilkThread.App.Services;

public interface INavigationService
{
    void Initialize(Frame frame);
    bool Navigate(Type pageType, object? parameter = null);
    bool GoBack();
    bool CanGoBack { get; }
}

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

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
