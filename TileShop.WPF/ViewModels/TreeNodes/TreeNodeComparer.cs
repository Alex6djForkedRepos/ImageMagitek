﻿using System.Collections.Generic;

namespace TileShop.WPF.ViewModels
{
    class TreeNodeComparer : IComparer<TreeNodeViewModel>
    {
        public int Compare(TreeNodeViewModel x, TreeNodeViewModel y)
        {
            if (x is FolderNodeViewModel && y is FolderNodeViewModel)
                return string.Compare(x.Node.Name, y.Node.Name);
            else if (x is FolderNodeViewModel)
                return -1;
            else if (y is FolderNodeViewModel)
                return 1;
            else
                return string.Compare(x.Node.Name, y.Node.Name);
        }
    }
}
