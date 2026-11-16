using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SilkThread.App.Helpers;

/// <summary>Stacks a two-column workspace according to its own available width.</summary>
public static class ResponsiveColumns
{
    public static readonly DependencyProperty BreakpointProperty = DependencyProperty.RegisterAttached(
        "Breakpoint", typeof(double), typeof(ResponsiveColumns), new PropertyMetadata(0d, Changed));
    public static double GetBreakpoint(DependencyObject obj) => (double)obj.GetValue(BreakpointProperty);
    public static void SetBreakpoint(DependencyObject obj, double value) => obj.SetValue(BreakpointProperty, value);

    private static void Changed(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        if (obj is not Grid grid) return;
        grid.SizeChanged -= Resize;
        if ((double)args.NewValue > 0) grid.SizeChanged += Resize;
    }

    private static void Resize(object sender, SizeChangedEventArgs args)
    {
        var grid = (Grid)sender;
        if (grid.ColumnDefinitions.Count != 2 || grid.Children.Count != 2) return;
        bool compact = args.NewSize.Width < GetBreakpoint(grid);
        if (grid.Children[0] is not FrameworkElement left || grid.Children[1] is not FrameworkElement right) return;
        if (grid.RowDefinitions.Count == 0)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }
        // Keep the original column ratios. A span in compact mode uses all available space.
        Grid.SetColumnSpan(left, compact ? 2 : 1);
        Grid.SetColumnSpan(right, compact ? 2 : 1);
        Grid.SetColumn(right, compact ? 0 : 1);
        Grid.SetRow(right, compact ? 1 : 0);
    }
}
