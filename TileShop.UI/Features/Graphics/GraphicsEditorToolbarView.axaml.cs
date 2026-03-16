using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using TileShop.UI.Models;
using TileShop.UI.ViewModels;

namespace TileShop.UI.Features.Graphics;

public partial class GraphicsEditorToolbarView : UserControl
{
    public GraphicsEditorToolbarView()
    {
        InitializeComponent();

        PaletteSwatchGrid.AddHandler(PointerPressedEvent, OnSwatchPointerPressed, handledEventsToo: true);
        PaletteSwatchGrid.ContextRequested += OnSwatchContextRequested;
    }

    private async void OnEditModeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is GraphicsEditMode mode && DataContext is GraphicsEditorViewModel vm)
        {
            // Undo the RadioButton's premature toggle before running the command
            SyncEditModeRadioButtons(vm.EditMode);
            await vm.ChangeEditModeCommand.ExecuteAsync(mode);
            SyncEditModeRadioButtons(vm.EditMode);
        }
    }

    private void SyncEditModeRadioButtons(GraphicsEditMode mode)
    {
        foreach (var child in EditModeGroup.Children)
        {
            if (child is RadioButton rb && rb.Tag is GraphicsEditMode rbMode)
                rb.SetCurrentValue(ToggleButton.IsCheckedProperty, rbMode == mode);
        }
    }

    private void OnSwatchContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnSwatchPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not GraphicsEditorViewModel vm)
            return;

        var point = e.GetCurrentPoint(sender as Visual);
        var source = e.Source as Visual;

        var border = source?.FindAncestorOfType<Border>();
        if (border?.DataContext is not PaletteEntry entry)
            return;

        if (point.Properties.IsLeftButtonPressed)
        {
            vm.SetPrimaryColorFromSwatchCommand.Execute(entry.Index);
            e.Handled = true;
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            vm.SetSecondaryColorFromSwatchCommand.Execute(entry.Index);
            e.Handled = true;
        }
    }
}
