﻿using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using ImageMagitek;
using ImageMagitek.Project;
using Monaco.PathTree;
using ImageMagitek.Colors;

namespace ImageMagitekConsole
{
    public class CommandProcessor
    {
        private readonly IPathTree<IProjectResource> _resourceTree;
        private readonly Palette _defaultPalette;

        public CommandProcessor(IPathTree<IProjectResource> resourceTree, Palette defaultPalette)
        {
            _resourceTree = resourceTree;
            _defaultPalette = defaultPalette;
            
        }

        public bool PrintResources()
        {
            foreach (var res in _resourceTree.EnumerateDepthFirst())
            {
                string key = res.PathKey;
                int level = key.Split('\\').Length;

                Console.WriteLine($"{res.Name}({level}): type '{res.Value.GetType().ToString()}' path '{key}'");
            }

            return true;
        }

        public bool ExportArranger(string arrangerKey, string projectRoot)
        {
            _resourceTree.TryGetNode(arrangerKey, out var node);

            var relativeFile = Path.Combine(node.Paths.ToArray());
            var exportFileName = Path.Combine(projectRoot, relativeFile + ".bmp");

            var arranger = node.Value as ScatteredArranger;

            Console.WriteLine($"Exporting {arranger.Name} to {exportFileName}...");
            Directory.CreateDirectory(Path.GetDirectoryName(exportFileName));

            if (arranger.ColorType == PixelColorType.Indexed)
            {
                var image = new IndexedImage(arranger, _defaultPalette);
                image.ExportImage(exportFileName, new ImageFileAdapter());
            }
            else if (arranger.ColorType == PixelColorType.Direct)
            {
                var image = new DirectImage(arranger);
                image.ExportImage(exportFileName, new ImageFileAdapter());
            }

            return true;
        }

        public bool ExportAllArrangers(string projectRoot)
        {
            foreach (var node in _resourceTree.EnumerateDepthFirst())
            {
                if(node.Value is ScatteredArranger)
                    ExportArranger(node.PathKey, projectRoot);
            }

            return true;
        }

        public bool ImportImage(string imageFileName, string arrangerKey)
        {
            Console.WriteLine($"Importing {imageFileName} to {arrangerKey}...");

            _resourceTree.TryGetValue(arrangerKey, out ScatteredArranger arranger);
            
            if (arranger.ColorType == PixelColorType.Indexed)
            {
                var image = new IndexedImage(arranger, _defaultPalette);
                image.ImportImage(imageFileName, new ImageFileAdapter(), ColorMatchStrategy.Exact);
                image.SaveImage();
            }
            else if (arranger.ColorType == PixelColorType.Direct)
            {
                var image = new DirectImage(arranger);
                image.ImportImage(imageFileName, new ImageFileAdapter());
                image.SaveImage();
            }

            return true;
        }

        public bool ImportAllImages(string projectRoot)
        {
            foreach (var node in _resourceTree.EnumerateDepthFirst().Where(x => x.Value is ScatteredArranger))
            {
                var arranger = node.Value as ScatteredArranger;
                var relativeFile = Path.Combine(node.Paths.ToArray());
                var imageFileName = Path.Combine(projectRoot, relativeFile + ".bmp");
                if(File.Exists(imageFileName))
                    ImportImage(imageFileName, node.PathKey);
            }
            return true;
        }

        public bool ResaveProject(string newProjectFile)
        {
            var writer = new XmlGameDescriptorWriter();
            return writer.WriteProject(_resourceTree, newProjectFile);
        }
    }
}
