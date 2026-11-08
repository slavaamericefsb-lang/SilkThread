using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SilkThread.App.Controls;

/// <summary>
/// High-performance responsive WrapPanel for WinUI 3.
/// Automatically wraps items into the next row/column when horizontal or vertical boundary is reached.
/// </summary>
public class WrapPanel : Panel
{
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(WrapPanel),
            new PropertyMetadata(Orientation.Horizontal, OnPropertyChangedInvalidateMeasure));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public static readonly DependencyProperty HorizontalSpacingProperty =
        DependencyProperty.Register(
            nameof(HorizontalSpacing),
            typeof(double),
            typeof(WrapPanel),
            new PropertyMetadata(8.0, OnPropertyChangedInvalidateMeasure));

    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public static readonly DependencyProperty VerticalSpacingProperty =
        DependencyProperty.Register(
            nameof(VerticalSpacing),
            typeof(double),
            typeof(WrapPanel),
            new PropertyMetadata(8.0, OnPropertyChangedInvalidateMeasure));

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    private static void OnPropertyChangedInvalidateMeasure(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WrapPanel panel)
        {
            panel.InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double curLineBreadth = 0;
        double curLineLength = 0;
        double totalBreadth = 0;
        double maxLineLength = 0;

        bool isHorizontal = Orientation == Orientation.Horizontal;
        double maxAvailableLength = isHorizontal ? availableSize.Width : availableSize.Height;

        var children = Children;
        int count = children.Count;

        for (int i = 0; i < count; i++)
        {
            var child = children[i];
            child.Measure(availableSize);
            Size childSize = child.DesiredSize;

            double childLength = isHorizontal ? childSize.Width : childSize.Height;
            double childBreadth = isHorizontal ? childSize.Height : childSize.Width;

            double spacing = isHorizontal ? HorizontalSpacing : VerticalSpacing;

            if (curLineLength + childLength > maxAvailableLength && curLineLength > 0)
            {
                // Wrap to next row
                totalBreadth += curLineBreadth + (isHorizontal ? VerticalSpacing : HorizontalSpacing);
                maxLineLength = Math.Max(maxLineLength, curLineLength);

                curLineLength = childLength;
                curLineBreadth = childBreadth;
            }
            else
            {
                if (curLineLength > 0)
                {
                    curLineLength += spacing;
                }
                curLineLength += childLength;
                curLineBreadth = Math.Max(curLineBreadth, childBreadth);
            }
        }

        totalBreadth += curLineBreadth;
        maxLineLength = Math.Max(maxLineLength, curLineLength);

        return isHorizontal ? new Size(maxLineLength, totalBreadth) : new Size(totalBreadth, maxLineLength);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        bool isHorizontal = Orientation == Orientation.Horizontal;
        double maxAvailableLength = isHorizontal ? finalSize.Width : finalSize.Height;

        double curLineBreadth = 0;
        double curLineLength = 0;
        double curBreadthOffset = 0;

        var children = Children;
        int count = children.Count;
        int lineStartIndex = 0;

        double lineSpacing = isHorizontal ? VerticalSpacing : HorizontalSpacing;
        double itemSpacing = isHorizontal ? HorizontalSpacing : VerticalSpacing;

        for (int i = 0; i < count; i++)
        {
            var child = children[i];
            Size childSize = child.DesiredSize;

            double childLength = isHorizontal ? childSize.Width : childSize.Height;
            double childBreadth = isHorizontal ? childSize.Height : childSize.Width;

            if (curLineLength + childLength > maxAvailableLength && curLineLength > 0)
            {
                // Arrange previous line
                ArrangeLine(lineStartIndex, i, curBreadthOffset, curLineBreadth, isHorizontal, itemSpacing);

                curBreadthOffset += curLineBreadth + lineSpacing;
                curLineLength = childLength;
                curLineBreadth = childBreadth;
                lineStartIndex = i;
            }
            else
            {
                if (curLineLength > 0)
                {
                    curLineLength += itemSpacing;
                }
                curLineLength += childLength;
                curLineBreadth = Math.Max(curLineBreadth, childBreadth);
            }
        }

        if (lineStartIndex < count)
        {
            ArrangeLine(lineStartIndex, count, curBreadthOffset, curLineBreadth, isHorizontal, itemSpacing);
        }

        return finalSize;
    }

    private void ArrangeLine(int startIndex, int endIndex, double breadthOffset, double lineBreadth, bool isHorizontal, double itemSpacing)
    {
        double lengthOffset = 0;
        var children = Children;

        for (int i = startIndex; i < endIndex; i++)
        {
            var child = children[i];
            Size childSize = child.DesiredSize;

            if (isHorizontal)
            {
                child.Arrange(new Rect(lengthOffset, breadthOffset, childSize.Width, childSize.Height));
                lengthOffset += childSize.Width + itemSpacing;
            }
            else
            {
                child.Arrange(new Rect(breadthOffset, lengthOffset, childSize.Width, childSize.Height));
                lengthOffset += childSize.Height + itemSpacing;
            }
        }
    }
}
