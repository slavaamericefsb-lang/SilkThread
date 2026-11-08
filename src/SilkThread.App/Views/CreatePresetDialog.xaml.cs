using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilkThread.App.Services;

namespace SilkThread.App.Views;

public sealed partial class CreatePresetDialog : ContentDialog
{
    public bool IsConfirmed { get; private set; }
    public string PresetName => PresetNameBox.Text.Trim();
    public string PresetDescription => PresetDescBox.Text.Trim();
    public string PresetAuthor => PresetAuthorBox.Text.Trim();

    public CreatePresetDialog(int activeTweaksCount)
    {
        InitializeComponent();
        var loc = LocalizationService.Instance;
        DialogTitleText.Text = loc["PresetCreate_Title"];
        DialogSubtitleText.Text = loc["PresetCreate_Subtitle"];
        BannerTitleText.Text = loc["PresetCreate_ConfigHeader"];
        BannerLabelText.Text = loc["PresetCreate_ActiveCount"];
        FieldTitleLabel.Text = loc["PresetCreate_NameLabel"];
        PresetNameBox.PlaceholderText = loc["PresetCreate_NamePlaceholder"];
        FieldDescLabel.Text = loc["PresetCreate_DescLabel"];
        PresetDescBox.PlaceholderText = loc["PresetCreate_DescPlaceholder"];
        FieldAuthorLabel.Text = loc["PresetCreate_AuthorLabel"];
        CancelButtonText.Text = loc["PresetCreate_CancelBtn"];
        SaveButtonText.Text = loc["PresetCreate_SaveBtn"];

        ActiveCountText.Text = activeTweaksCount.ToString();
        PresetNameBox.Text = $"SilkPreset_{DateTime.Now:yyyyMMdd}";
        PresetDescBox.Text = loc["PresetCreate_DefaultDesc"];
        PresetAuthorBox.Text = Environment.UserName;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Hide();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PresetNameBox.Text))
        {
            PresetNameBox.Text = $"SilkPreset_{DateTime.Now:yyyyMMdd}";
        }
        IsConfirmed = true;
        Hide();
    }
}
