﻿using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ImageMagitek;
using TileShop.AvaloniaUI.Models;
using TileShop.AvaloniaUI.ViewModels;

namespace TileShop.AvaloniaUI.DragDrop;
public class ArrangerDropHandler : DropHandlerBase
{
    public override void Over(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
    {
        if (sourceContext is ArrangerPaste paste &&
            targetContext is ArrangerEditorViewModel targetVm &&
            sender is IControl control)
        {
            if (paste.Copy is ElementCopy && !targetVm.CanAcceptElementPastes)
                return;

            if ((paste.Copy is IndexedPixelCopy || paste.Copy is DirectPixelCopy) && !targetVm.CanAcceptPixelPastes)
                return;

            targetVm.Paste = paste;

            var p = e.GetPosition(control);
            targetVm.Paste.MoveTo((int)p.X, (int)p.Y);

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
        if (sender is IControl { DataContext: ArrangerEditorViewModel vm } control)
        {
            vm.CancelOverlay();
        }
        base.Leave(sender, e);
    }

    public override bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (sender is IControl && sourceContext is ArrangerPaste && targetContext is ArrangerEditorViewModel)
        {
            return true;
        }
        return false;
    }

    public override bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
    {
        if (e.Source is IControl &&
            sourceContext is ArrangerPaste paste &&
            targetContext is ArrangerEditorViewModel targetVm &&
            sender is IControl control
            && Validate(control, e, sourceContext, targetContext, state))
        {
            paste.IsDragging = false;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                targetVm.CompletePaste();
            }
            else
            {
                targetVm.PendingOperationMessage = $"Press [Enter] to Apply Paste or [Esc] to Cancel";
            }

            control.Focus();
            return true;
        }

        return false;
    }
}