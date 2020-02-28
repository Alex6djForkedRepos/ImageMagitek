﻿using System;
using System.Windows;
using System.Windows.Controls;
using TileShop.WPF.ViewModels;

namespace TileShop.WPF.Selectors
{
    public class ProjectTreeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TreeProjectTemplate { get; set; }
        public DataTemplate TreeFolderTemplate { get; set; }
        public DataTemplate TreeArrangerTemplate { get; set; }
        public DataTemplate TreeDataFileTemplate { get; set; }
        public DataTemplate TreePaletteTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null || container is null)
                return null;

            return item switch
            {
                ImageProjectNodeViewModel _ => TreeProjectTemplate,
                FolderNodeViewModel _ => TreeFolderTemplate,
                DataFileNodeViewModel _ => TreeDataFileTemplate,
                ArrangerNodeViewModel _ => TreeArrangerTemplate,
                PaletteNodeViewModel _ => TreePaletteTemplate,
                _ => throw new ArgumentException($"{nameof(SelectTemplate)} does not contain a template for type {item.GetType()}")
            };
        }
    }
}
