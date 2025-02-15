﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace VoicemeeterOsdProgram.UiControls
{
    class WrapPanelExt : WrapPanel
    {
        public uint Wraps
        {
            get
            {
                // PROBLEM: width can be limited just by the parent,
                // and RenderSize, Actual* return incorrect value. Bug ticket rejected in WPF's github
                var maxWidth = (Orientation == Orientation.Horizontal) ? MaxWidth : MaxHeight;
                if (maxWidth is double.NaN) throw new ArgumentNullException("MaxWidth/MaxHeight must be specified");

                double width = 0;
                foreach (FrameworkElement child in Children)
                {
                    width += child.ActualWidth;
                }
                return (uint)(width / maxWidth);
            }
        }

        public IEnumerable<IEnumerable<UIElement>> GetChildrenLines()
        {
            var len = Children.Count;
            List<IEnumerable<UIElement>> lines = new();
            List<UIElement> line = new();
            lines.Add(line);
            if (len == 0) return lines;

            var isHorizontal = Orientation == Orientation.Horizontal;
            // PROBLEM: width can be limited just by the parent,
            // and RenderSize, Actual* return incorrect value. Bug ticket rejected in WPF's github
            double maxWidth = isHorizontal ? MaxWidth : MaxHeight;
            if (maxWidth is double.NaN) throw new ArgumentNullException("MaxWidth/MaxHeight must be specified");
            
            double lineW = 0;
            foreach (FrameworkElement child in Children)
            {
                var W = isHorizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
                //var W = isHorizontal ? child.ActualWidth : child.ActualHeight;
                lineW += W;
                var isOnNewLine = (lineW / maxWidth) > 1;
                if (isOnNewLine)
                {
                    line = new();
                    lines.Add(line);
                    lineW = 0 + W;
                }
                line.Add(child);
            }
            return lines;
        }
    }
}
