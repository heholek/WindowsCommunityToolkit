﻿// <copyright file="WindowsXamlHostLayout.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <author>Microsoft</author>

namespace Microsoft.Windows.Interop
{
    using System;
    using System.Windows;
    using System.Windows.Interop;

    partial class WindowsXamlHost : HwndHost
    {
        #region Layout

        /// <summary>
        /// Measures wrapped UWP XAML content using passed in size constraint
        /// </summary>
        /// <param name="availableSize">Available Size</param>
        /// <returns>XAML DesiredSize</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size desiredSize = new Size(0, 0);

            if (this.desktopWindowXamlSource.Content != null)
            {
                this.desktopWindowXamlSource.Content.Measure(new global::Windows.Foundation.Size(availableSize.Width, availableSize.Height));
                desiredSize.Width = this.desktopWindowXamlSource.Content.DesiredSize.Width;
                desiredSize.Height = this.desktopWindowXamlSource.Content.DesiredSize.Height;
            }

            desiredSize.Width = Math.Min(desiredSize.Width, availableSize.Width);
            desiredSize.Height = Math.Min(desiredSize.Height, availableSize.Height);

            return desiredSize;
        }

        /// <summary>
        /// Arranges wrapped UWP XAML content using passed in size constraint
        /// </summary>
        /// <param name="finalSize">Final Size</param>
        /// <returns>Size</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            
            if (this.desktopWindowXamlSource.Content != null)
            {
                // Arrange is required to support HorizontalAlignment and VerticalAlignment properties
                // set to 'Stretch'.  The UWP XAML content will be 0 in the stretch alignment direction
                // until Arrange is called, and the UWP XAML content is expanded to fill the available space.
                global::Windows.Foundation.Rect finalRect = new global::Windows.Foundation.Rect(0, 0, finalSize.Width, finalSize.Height);
                this.desktopWindowXamlSource.Content.Arrange(finalRect);
            } 

            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Notifies host control when wrapped UWP XAML content has become dirty and performed a layout pass
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Object</param>
        private void XamlContentLayoutUpdated(object sender, object e)
        {
            // UWP XAML content has changed. Force parent control to re-measure.
            InvalidateMeasure();
        }

        /// <summary>
        /// UWP XAML content size changed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">SizeChangedEventArgs</param>
        private void XamlContentSizeChanged(object sender, global::Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            this.ParentLayoutInvalidated(this);
        }

        #endregion
    }
}