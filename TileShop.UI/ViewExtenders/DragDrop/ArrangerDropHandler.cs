using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using Dock.Avalonia.Controls;
using Dock.Model.Core;
using ImageMagitek;
using TileShop.Shared.Models;
using TileShop.Shared.Tools;
using TileShop.UI.Controls;
using TileShop.UI.Models;
using TileShop.UI.ViewModels;
using TileShop.UI.Views;

namespace TileShop.UI.DragDrop;
public class ArrangerDropHandler : DropHandlerBase
{
    public override void Over(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        if (sourceContext is ArrangerPaste paste &&
            targetContext is GraphicsEditorViewModel targetVm &&
            sender is Control control)
        {
            if (paste.Copy is ElementCopy && !targetVm.CanAcceptElementPastes)
                return;

            if (paste.Copy is IndexedPixelCopy or DirectPixelCopy && !targetVm.CanAcceptPixelPastes)
                return;

            targetVm.Paste = paste;

            paste.SnapMode = targetVm.EditMode == GraphicsEditMode.Arrange
                ? SnapMode.Element
                : SnapMode.Pixel;

            var screenPos = e.GetPosition(control);
            var p = control is InfiniteCanvas canvas
                ? canvas.ScreenToLocalPoint(screenPos)
                : screenPos;
            targetVm.Paste.MoveTo((int)p.X, (int)p.Y);
            targetVm.ClampPastePositionToDrawClip();
            targetVm.InvalidateEditor(InvalidationLevel.Overlay);

            e.Handled = true;
        }
        else
            base.Over(sender, e, sourceContext, targetContext);
    }

    public override void Enter(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        base.Enter(sender, e, sourceContext, targetContext);
    }

    public override void Leave(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: GraphicsEditorViewModel vm } control)
        {
            vm.CancelOverlay();
        }
        base.Leave(sender, e);
    }

    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (sender is Control && sourceContext is ArrangerPaste && targetContext is GraphicsEditorViewModel)
        {
            return true;
        }
        return false;
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (e.Source is Control &&
            sourceContext is ArrangerPaste paste &&
            targetContext is GraphicsEditorViewModel targetVm &&
            sender is Control control
            && Validate(control, e, sourceContext, targetContext, state))
        {
            paste.IsDragging = false;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                targetVm.CompletePaste();
            }
            else
            {
                var pasteType = targetVm.IsElementPasteActive ? "Element" : "Pixel";
                targetVm.PendingOperationMessage = $"Press [Enter] to Apply {pasteType} Paste or [Esc] to Cancel";
            }

            // Activate the dock tab and focus the editor so key bindings (Enter/Esc) work
            ActivateEditorDockTab(control);
            return true;
        }

        return false;
    }

    private static void ActivateEditorDockTab(Control control)
    {
        // Find the DockableEditorViewModel by walking up the visual tree
        DockableEditorViewModel? dockableVm = null;
        Control? view = control;
        
        foreach (var ancestor in control.GetVisualAncestors())
        {
            if (ancestor is Control { DataContext: DockableEditorViewModel dvm })
            {
                dockableVm = dvm;
                break;
            }
        }

        // Activate the dockable so the tab gets the :active pseudoclass
        if (dockableVm is not null)
        {
            var dockControl = control.FindAncestorOfType<DockControl>();
            if (dockControl?.Factory is { } factory)
            {
                factory.SetActiveDockable(dockableVm);
                if (dockableVm.Owner is IDock owner)
                    factory.SetFocusedDockable(owner, dockableVm);
            }
        }

        foreach (var ancestor in control.GetVisualAncestors())
        {
            if (ancestor is GraphicsEditorView gev)
            {
                gev.Focus();
            }
        }
    }
}
