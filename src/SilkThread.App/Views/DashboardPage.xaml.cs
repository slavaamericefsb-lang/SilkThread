using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using SilkThread.App.Services;
using SilkThread.App.ViewModels;
using Windows.UI;

namespace SilkThread.App.Views;

public sealed partial class DashboardPage : Page, IStaggeredPage
{
    public DashboardViewModel ViewModel { get; }

    public IReadOnlyList<FrameworkElement> GetStaggeredElements()
    {
        return new FrameworkElement[]
        {
            HeaderGrid,
            Card1Optimization,
            Card3Hardware,
            Card2Iost,
            Card4Latency,
            Card5Security,
            Card6Presets
        };
    }

    private static readonly SolidColorBrush s_tileHoverBorder = new(Color.FromArgb(255, 128, 107, 255)); // #806BFF
    private static readonly SolidColorBrush s_tileNormalBorder = new(Color.FromArgb(255, 43, 48, 64));   // #2B3040
    private static readonly SolidColorBrush s_tileHoverBg = new(Color.FromArgb(255, 37, 42, 59));       // #252A3B
    private static readonly SolidColorBrush s_tileNormalBg = new(Color.FromArgb(255, 32, 36, 49));      // #202431

    public DashboardPage()
    {
        ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        PageTransitionHelper.AnimatePageEntrance(this);
        await ViewModel.InitializeAsync();
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        double width = e.NewSize.Width;

        // 1. Adaptive Page Header (Drop action buttons below title on < 1120px)
        ApplyHeaderLayout(width);

        // 2. Bento Grid: 2 balanced columns with synchronized row heights on >= 1120px vs single-column stack on < 1120px
        ApplyBentoGridLayout(width);

        // 3. IOST Card Header (Prevents collision between title and badge on compact windows)
        ApplyAiHeaderLayout(width);

        // 4. Score Card: Dial and Description layout (Dial centered on top on < 780px)
        ApplyScoreDialLayout(width);

        // 5. Hardware & Latency Grids (2x2 on Desktop, 1-Column stack on narrow displays)
        ApplySubModulesLayout(width);
    }

    private void ApplyHeaderLayout(double width)
    {
        if (HeaderTitlePanel == null || HeaderActionsPanel == null) return;

        if (width < 1120)
        {
            // Drop action buttons to Row 1 beneath the title
            Grid.SetRow(HeaderTitlePanel, 0);
            Grid.SetColumn(HeaderTitlePanel, 0);
            Grid.SetColumnSpan(HeaderTitlePanel, 2);

            Grid.SetRow(HeaderActionsPanel, 1);
            Grid.SetColumn(HeaderActionsPanel, 0);
            Grid.SetColumnSpan(HeaderActionsPanel, 2);
            HeaderActionsPanel.HorizontalAlignment = HorizontalAlignment.Left;
            HeaderActionsPanel.Margin = new Thickness(0, 10, 0, 0);
        }
        else
        {
            // Full width: Side-by-side title on left, buttons on right
            Grid.SetRow(HeaderTitlePanel, 0);
            Grid.SetColumn(HeaderTitlePanel, 0);
            Grid.SetColumnSpan(HeaderTitlePanel, 1);

            Grid.SetRow(HeaderActionsPanel, 0);
            Grid.SetColumn(HeaderActionsPanel, 1);
            Grid.SetColumnSpan(HeaderActionsPanel, 1);
            HeaderActionsPanel.HorizontalAlignment = HorizontalAlignment.Right;
            HeaderActionsPanel.Margin = new Thickness(0);
        }
    }

    private void ApplyBentoGridLayout(double width)
    {
        if (BentoCol0 == null || BentoCol1 == null || 
            Card1Optimization == null || Card3Hardware == null || 
            Card2Iost == null || Card4Latency == null || 
            Card5Security == null || Card6Presets == null) return;

        if (width < 1120)
        {
            // Compact / narrow window: single-column vertical flow (ColSpan 2)
            BentoCol0.Width = new GridLength(1, GridUnitType.Star);
            BentoCol1.Width = new GridLength(0);

            Grid.SetRow(Card1Optimization, 0); Grid.SetColumn(Card1Optimization, 0); Grid.SetColumnSpan(Card1Optimization, 2);
            Grid.SetRow(Card3Hardware, 1);     Grid.SetColumn(Card3Hardware, 0);     Grid.SetColumnSpan(Card3Hardware, 2);
            Grid.SetRow(Card2Iost, 2);         Grid.SetColumn(Card2Iost, 0);         Grid.SetColumnSpan(Card2Iost, 2);
            Grid.SetRow(Card4Latency, 3);      Grid.SetColumn(Card4Latency, 0);      Grid.SetColumnSpan(Card4Latency, 2);
            Grid.SetRow(Card5Security, 4);     Grid.SetColumn(Card5Security, 0);     Grid.SetColumnSpan(Card5Security, 2);
            Grid.SetRow(Card6Presets, 5);      Grid.SetColumn(Card6Presets, 0);      Grid.SetColumnSpan(Card6Presets, 2);
        }
        else
        {
            // Wide displays: 2 balanced Bento Grid columns with perfectly equalized row heights (50/50 symmetry)
            BentoCol0.Width = new GridLength(1, GridUnitType.Star);
            BentoCol1.Width = new GridLength(1, GridUnitType.Star);

            // Row 0: Score & Hardware (synchronized row height)
            Grid.SetRow(Card1Optimization, 0); Grid.SetColumn(Card1Optimization, 0); Grid.SetColumnSpan(Card1Optimization, 1);
            Grid.SetRow(Card3Hardware, 0);     Grid.SetColumn(Card3Hardware, 1);     Grid.SetColumnSpan(Card3Hardware, 1);

            // Row 1: IOST & Latency Matrix (synchronized row height)
            Grid.SetRow(Card2Iost, 1);         Grid.SetColumn(Card2Iost, 0);         Grid.SetColumnSpan(Card2Iost, 1);
            Grid.SetRow(Card4Latency, 1);      Grid.SetColumn(Card4Latency, 1);      Grid.SetColumnSpan(Card4Latency, 1);

            // Row 2: Security & Presets (synchronized row height)
            Grid.SetRow(Card5Security, 2);     Grid.SetColumn(Card5Security, 0);     Grid.SetColumnSpan(Card5Security, 1);
            Grid.SetRow(Card6Presets, 2);      Grid.SetColumn(Card6Presets, 1);      Grid.SetColumnSpan(Card6Presets, 1);
        }
    }

    private void ApplyAiHeaderLayout(double width)
    {
        if (AiHeaderTitlePanel == null || AiHeaderBadgesPanel == null) return;

        if (width < 640)
        {
            // Stack badge below title
            Grid.SetRow(AiHeaderTitlePanel, 0);
            Grid.SetColumn(AiHeaderTitlePanel, 0);
            Grid.SetColumnSpan(AiHeaderTitlePanel, 2);

            Grid.SetRow(AiHeaderBadgesPanel, 1);
            Grid.SetColumn(AiHeaderBadgesPanel, 0);
            Grid.SetColumnSpan(AiHeaderBadgesPanel, 2);
            AiHeaderBadgesPanel.Margin = new Thickness(0, 8, 0, 0);
        }
        else
        {
            // Side-by-side in header
            Grid.SetRow(AiHeaderTitlePanel, 0);
            Grid.SetColumn(AiHeaderTitlePanel, 0);
            Grid.SetColumnSpan(AiHeaderTitlePanel, 1);

            Grid.SetRow(AiHeaderBadgesPanel, 0);
            Grid.SetColumn(AiHeaderBadgesPanel, 1);
            Grid.SetColumnSpan(AiHeaderBadgesPanel, 1);
            AiHeaderBadgesPanel.Margin = new Thickness(0);
        }
    }

    private void ApplyScoreDialLayout(double width)
    {
        if (ScoreDialContainer == null || ScoreInfoContainer == null || ScoreDescriptionText == null) return;

        if (width < 780)
        {
            // Vertical stacked layout: Dial centered on top, full width for description and chips
            Grid.SetRow(ScoreDialContainer, 0);
            Grid.SetColumn(ScoreDialContainer, 0);
            Grid.SetColumnSpan(ScoreDialContainer, 2);
            ScoreDialContainer.HorizontalAlignment = HorizontalAlignment.Center;
            ScoreDialContainer.Margin = new Thickness(0, 0, 0, 10);

            Grid.SetRow(ScoreInfoContainer, 1);
            Grid.SetColumn(ScoreInfoContainer, 0);
            Grid.SetColumnSpan(ScoreInfoContainer, 2);
            ScoreInfoContainer.HorizontalAlignment = HorizontalAlignment.Stretch;

            ScoreDescriptionText.TextAlignment = TextAlignment.Center;
        }
        else
        {
            // Horizontal side-by-side layout
            Grid.SetRow(ScoreDialContainer, 0);
            Grid.SetColumn(ScoreDialContainer, 0);
            Grid.SetColumnSpan(ScoreDialContainer, 1);
            ScoreDialContainer.HorizontalAlignment = HorizontalAlignment.Left;
            ScoreDialContainer.Margin = new Thickness(0);

            Grid.SetRow(ScoreInfoContainer, 0);
            Grid.SetColumn(ScoreInfoContainer, 1);
            Grid.SetColumnSpan(ScoreInfoContainer, 1);
            ScoreInfoContainer.HorizontalAlignment = HorizontalAlignment.Stretch;

            ScoreDescriptionText.TextAlignment = TextAlignment.Left;
        }
    }

    private void ApplySubModulesLayout(double width)
    {
        // Hardware 2x2 vs 1-Column Stack
        if (HwCol0 != null && HwCol1 != null && TileCpu != null && TileGpu != null && TileRam != null && TileOs != null)
        {
            if (width < 640)
            {
                HwCol0.Width = new GridLength(1, GridUnitType.Star);
                HwCol1.Width = new GridLength(0);

                Grid.SetRow(TileCpu, 0); Grid.SetColumn(TileCpu, 0); Grid.SetColumnSpan(TileCpu, 2);
                Grid.SetRow(TileGpu, 1); Grid.SetColumn(TileGpu, 0); Grid.SetColumnSpan(TileGpu, 2);
                Grid.SetRow(TileRam, 2); Grid.SetColumn(TileRam, 0); Grid.SetColumnSpan(TileRam, 2);
                Grid.SetRow(TileOs, 3);  Grid.SetColumn(TileOs, 0);  Grid.SetColumnSpan(TileOs, 2);
            }
            else
            {
                HwCol0.Width = new GridLength(1, GridUnitType.Star);
                HwCol1.Width = new GridLength(1, GridUnitType.Star);

                Grid.SetRow(TileCpu, 0); Grid.SetColumn(TileCpu, 0); Grid.SetColumnSpan(TileCpu, 1);
                Grid.SetRow(TileGpu, 0); Grid.SetColumn(TileGpu, 1); Grid.SetColumnSpan(TileGpu, 1);
                Grid.SetRow(TileRam, 1); Grid.SetColumn(TileRam, 0); Grid.SetColumnSpan(TileRam, 1);
                Grid.SetRow(TileOs, 1);  Grid.SetColumn(TileOs, 1);  Grid.SetColumnSpan(TileOs, 1);
            }
        }

        // Latency 2x2 vs 1-Column Stack
        if (LatencyCol0 != null && LatencyCol1 != null && ModRawInput != null && ModHags != null && ModTcp != null && ModMmcss != null)
        {
            if (width < 640)
            {
                LatencyCol0.Width = new GridLength(1, GridUnitType.Star);
                LatencyCol1.Width = new GridLength(0);

                Grid.SetRow(ModRawInput, 0); Grid.SetColumn(ModRawInput, 0); Grid.SetColumnSpan(ModRawInput, 2);
                Grid.SetRow(ModHags, 1);     Grid.SetColumn(ModHags, 0);     Grid.SetColumnSpan(ModHags, 2);
                Grid.SetRow(ModTcp, 2);      Grid.SetColumn(ModTcp, 0);      Grid.SetColumnSpan(ModTcp, 2);
                Grid.SetRow(ModMmcss, 3);    Grid.SetColumn(ModMmcss, 0);    Grid.SetColumnSpan(ModMmcss, 2);
            }
            else
            {
                LatencyCol0.Width = new GridLength(1, GridUnitType.Star);
                LatencyCol1.Width = new GridLength(1, GridUnitType.Star);

                Grid.SetRow(ModRawInput, 0); Grid.SetColumn(ModRawInput, 0); Grid.SetColumnSpan(ModRawInput, 1);
                Grid.SetRow(ModHags, 0);     Grid.SetColumn(ModHags, 1);     Grid.SetColumnSpan(ModHags, 1);
                Grid.SetRow(ModTcp, 1);      Grid.SetColumn(ModTcp, 0);      Grid.SetColumnSpan(ModTcp, 1);
                Grid.SetRow(ModMmcss, 1);    Grid.SetColumn(ModMmcss, 1);    Grid.SetColumnSpan(ModMmcss, 1);
            }
        }
    }

    private readonly Dictionary<string, Storyboard> _tileStoryboards = new();

    private void LatencyTile_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is string tag)
        {
            AnimateTileHover(border, tag, isHovered: true);
        }
    }

    private void LatencyTile_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is string tag)
        {
            AnimateTileHover(border, tag, isHovered: false);
        }
    }

    private void AnimateTileHover(Border border, string tag, bool isHovered)
    {
        if (_tileStoryboards.TryGetValue(tag, out var existingSb))
        {
            existingSb.Stop();
            _tileStoryboards.Remove(tag);
        }

        var sb = new Storyboard();
        var duration = TimeSpan.FromMilliseconds(180);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        // 1. Smooth Background color transition: #202431 <-> #252A3C
        var targetBgColor = isHovered
            ? Color.FromArgb(255, 37, 42, 60)   // #252A3C
            : Color.FromArgb(255, 32, 36, 49);  // #202431
        if (border.Background is SolidColorBrush bgBrush)
        {
            var bgAnim = new ColorAnimation
            {
                To = targetBgColor,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(bgAnim, bgBrush);
            Storyboard.SetTargetProperty(bgAnim, "Color");
            sb.Children.Add(bgAnim);
        }

        // 2. Smooth BorderBrush color transition: #2B3040 <-> #806BFF
        var targetBorderColor = isHovered
            ? Color.FromArgb(255, 128, 107, 255) // #806BFF
            : Color.FromArgb(255, 43, 48, 64);    // #2B3040
        if (border.BorderBrush is SolidColorBrush borderBrush)
        {
            var borderAnim = new ColorAnimation
            {
                To = targetBorderColor,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(borderAnim, borderBrush);
            Storyboard.SetTargetProperty(borderAnim, "Color");
            sb.Children.Add(borderAnim);
        }

        // 3. Smooth Opacity crossfade between Subtitle and Detailed description
        var (sub, detail) = GetTileTextElements(tag);
        if (sub != null)
        {
            var subAnim = new DoubleAnimation
            {
                To = isHovered ? 0.0 : 1.0,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(subAnim, sub);
            Storyboard.SetTargetProperty(subAnim, "Opacity");
            sb.Children.Add(subAnim);
        }

        if (detail != null)
        {
            var detailAnim = new DoubleAnimation
            {
                To = isHovered ? 1.0 : 0.0,
                Duration = duration,
                EasingFunction = ease
            };
            Storyboard.SetTarget(detailAnim, detail);
            Storyboard.SetTargetProperty(detailAnim, "Opacity");
            sb.Children.Add(detailAnim);
        }

        _tileStoryboards[tag] = sb;
        sb.Completed += (s, e) =>
        {
            _tileStoryboards.Remove(tag);
            if (border.Background is SolidColorBrush b) b.Color = targetBgColor;
            if (border.BorderBrush is SolidColorBrush br) br.Color = targetBorderColor;
            if (sub != null) sub.Opacity = isHovered ? 0.0 : 1.0;
            if (detail != null) detail.Opacity = isHovered ? 1.0 : 0.0;
        };
        sb.Begin();
    }

    private (UIElement? Subtitle, UIElement? Detail) GetTileTextElements(string tag) => tag switch
    {
        "raw_input" => (SubRawInput, DetailRawInput),
        "hags" => (SubHags, DetailHags),
        "tcp_nodelay" => (SubTcp, DetailTcp),
        "mmcss" => (SubMmcss, DetailMmcss),
        _ => (null, null)
    };

    private void LatencyTile_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is string tag)
        {
            ViewModel.ToggleLatencyTile(tag);
        }
    }

    private void AiScenario_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AiSurveyOptionItem item)
        {
            ViewModel.SelectAiScenario(item);
        }
    }

    private void AiPriority_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AiSurveyOptionItem item)
        {
            ViewModel.SelectAiPriority(item);
        }
    }
}
