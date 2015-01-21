﻿using System;
using System.ComponentModel;
using TwistedLogik.Ultraviolet.Graphics.Graphics2D.Text;
using TwistedLogik.Ultraviolet.UI.Presentation.Animations;
using TwistedLogik.Ultraviolet.UI.Presentation.Styles;

namespace TwistedLogik.Ultraviolet.UI.Presentation.Elements
{
    /// <summary>
    /// Represents a control which displays a single item of content.
    /// </summary>
    [DefaultProperty("Content")]
    public abstract class ContentControl : Control
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentControl"/> class.
        /// </summary>
        /// <param name="uv">The Ultraviolet context.</param>
        /// <param name="id">The unique identifier of this element within its layout.</param>
        public ContentControl(UltravioletContext uv, String id)
            : base(uv, id)
        {

        }

        /// <inheritdoc/>
        public override UIElement GetLogicalChild(int ix)
        {
            if (contentElement == null || ix < 0 || ix > 1)
                throw new ArgumentOutOfRangeException("ix");

            return contentElement;
        }

        /// <summary>
        /// Gets or sets the control's content.
        /// </summary>
        public Object Content
        {
            get { return GetValue<Object>(ContentProperty); }
            set { SetValue<Object>(ContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the control's content.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue<HorizontalAlignment>(HorizontalContentAlignmentProperty); }
            set { SetValue<HorizontalAlignment>(HorizontalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the control's content.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue<VerticalAlignment>(VerticalContentAlignmentProperty); }
            set { SetValue<VerticalAlignment>(VerticalContentAlignmentProperty, value); }
        }

        /// <inheritdoc/>
        public override Int32 LogicalChildren
        {
            get { return contentElement == null ? 0 : 1; }
        }

        /// <summary>
        /// Occurs when the value of the <see cref="HorizontalContentAlignment"/> property changes.
        /// </summary>
        public event UIElementEventHandler HorizontalContentAlignmentChanged;

        /// <summary>
        /// Occurs when the value of the <see cref="VerticalContentAlignment"/> property changes.
        /// </summary>
        public event UIElementEventHandler VerticalContentAlignmentChanged;

        /// <summary>
        /// Occurs when the value of the <see cref="Content"/> property changes.
        /// </summary>
        public event UIElementEventHandler ContentChanged;

        /// <summary>
        /// Identifies the <see cref="HorizontalContentAlignment"/> dependency property.
        /// </summary>
        [Styled("content-halign")]
        public static readonly DependencyProperty HorizontalContentAlignmentProperty = DependencyProperty.Register("HorizontalContentAlignment", typeof(HorizontalAlignment), typeof(ContentControl),
            new DependencyPropertyMetadata(HandleHorizontalContentAlignmentChanged, () => HorizontalAlignment.Left, DependencyPropertyOptions.AffectsArrange));

        /// <summary>
        /// Identifies the <see cref="VerticalContentAlignment"/> dependency property.
        /// </summary>
        [Styled("content-valign")]
        public static readonly DependencyProperty VerticalContentAlignmentProperty = DependencyProperty.Register("VerticalContentAlignment", typeof(VerticalAlignment), typeof(ContentControl),
            new DependencyPropertyMetadata(HandleVerticalContentAlignmentChanged, () => VerticalAlignment.Top, DependencyPropertyOptions.AffectsArrange));

        /// <summary>
        /// Identifies the <see cref="Content"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(Object), typeof(ContentControl), 
            new DependencyPropertyMetadata(HandleContentChanged, null, DependencyPropertyOptions.AffectsMeasure));

        /// <inheritdoc/>
        protected internal override void RemoveChild(UIElement child)
        {
            if (Content == child)
            {
                Content = null;
            }
            base.RemoveChild(child);
        }

        /// <inheritdoc/>
        protected override void ReloadContentCore(Boolean recursive)
        {
            if (recursive && contentElement != null)
            {
                contentElement.ReloadContent(true);
            }
            base.ReloadContentCore(recursive);
        }

        /// <inheritdoc/>
        protected override void ClearAnimationsCore(Boolean recursive)
        {
            if (recursive && contentElement != null)
            {
                contentElement.ClearAnimations(true);
            }
            base.ClearAnimationsCore(recursive);
        }

        /// <inheritdoc/>
        protected override void ClearLocalValuesCore(Boolean recursive)
        {
            if (recursive && contentElement != null)
            {
                contentElement.ClearLocalValues(true);
            }
            base.ClearLocalValuesCore(recursive);
        }

        /// <inheritdoc/>
        protected override void ClearStyledValuesCore(Boolean recursive)
        {
            if (recursive && contentElement != null)
            {
                contentElement.ClearStyledValues(true);
            }
            base.ClearStyledValuesCore(recursive);
        }

        /// <inheritdoc/>
        protected override void CleanupCore()
        {
            if (contentElement != null)
            {
                contentElement.Cleanup();
            }
            base.CleanupCore();
        }

        /// <inheritdoc/>
        protected override void CacheLayoutParametersCore()
        {
            if (contentElement != null)
            {
                contentElement.CacheLayoutParameters();
            }
            base.CacheLayoutParametersCore();
        }

        /// <inheritdoc/>
        protected override void AnimateCore(Storyboard storyboard, StoryboardClock clock, UIElement root)
        {
            if (contentElement != null)
            {
                contentElement.Animate(storyboard, clock, root);
            }
            base.AnimateCore(storyboard, clock, root);
        }

        /// <inheritdoc/>        
        protected override void StyleOverride(UvssDocument stylesheet)
        {
            if (contentElement != null)
            {
                contentElement.Style(stylesheet);
            }
            base.StyleOverride(stylesheet);
        }

        /// <inheritdoc/>
        protected override Size2D MeasureContent(Size2D availableSize)
        {
            if (contentElement != null)
            {
                var desiredContentSize = new Size2D(DesiredContentRegion.Width, DesiredContentRegion.Height);
                contentElement.Measure(desiredContentSize);
                return GetTotalContentPadding() + contentElement.DesiredSize;
            }
            else
            {
                UpdateTextLayoutCache(availableSize);

                var dipsWidth  = Display.PixelsToDips(textLayoutResult.ActualWidth);
                var dipsHeight = Display.PixelsToDips(textLayoutResult.ActualHeight);
                var dipsSize   = new Size2D(dipsWidth, dipsHeight);

                return GetTotalContentPadding() + dipsSize;
            }
        }

        /// <inheritdoc/>
        protected override Size2D ArrangeContent(Size2D finalSize, ArrangeOptions options)
        {
            var padding = GetTotalContentPadding();

            var contentSpace = finalSize - padding;

            if (contentElement != null)
            {
                var contentSize = contentElement.DesiredSize;

                var contentX = LayoutUtil.PerformHorizontalAlignment(contentSpace, contentSize, HorizontalContentAlignment);
                var contentY = LayoutUtil.PerformVerticalAlignment(contentSpace, contentSize, VerticalContentAlignment);

                var contentPosition = new Point2D(contentX, contentY);
                var contentRegion   = new RectangleD(contentPosition, contentSize);

                contentElement.Arrange(contentRegion);
            }
            else
            {
                UpdateTextLayoutCache(contentSpace);
            }
            return finalSize;
        }

        /// <inheritdoc/>
        protected override void PositionContent(Point2D position)
        {
            if (contentElement != null)
            {
                contentElement.Position(AbsolutePosition);
            }
        }

        /// <inheritdoc/>
        protected override RectangleD? ClipContentCore()
        {
            if (contentElement != null)
            {
                if (contentElement.RelativeBounds.Left < 0 || contentElement.RelativeBounds.Y < 0 ||
                    contentElement.RelativeBounds.Right > RenderContentRegion.Width ||
                    contentElement.RelativeBounds.Bottom > RenderContentRegion.Height)
                {
                    return AbsoluteContentRegion;
                }
            }
            else
            {
                if (textLayoutResult.ActualWidth > RenderContentRegion.Width || 
                    textLayoutResult.ActualHeight > RenderContentRegion.Height)
                {
                    return AbsoluteContentRegion;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override UIElement GetElementAtPointCore(Double x, Double y, Boolean isHitTest)
        {
            if (contentElement != null)
            {
                var contentRelX = x - contentElement.RelativeBounds.X;
                var contentRelY = y - contentElement.RelativeBounds.Y;

                var contentMatch = contentElement.GetElementAtPoint(contentRelX, contentRelY, isHitTest);
                if (contentMatch != null)
                {
                    return contentMatch;
                }
            }
            return base.GetElementAtPointCore(x, y, isHitTest);
        }

        /// <inheritdoc/>
        protected override void DrawContent(UltravioletTime time, DrawingContext dc)
        {
            if (Content != null)
            {
                var clip = ClipContentRectangle;
                if (clip != null)
                    dc.PushClipRectangle(clip.Value);

                if (contentElement != null)
                {
                    contentElement.Draw(time, dc);
                }
                else
                {
                    if (Content != null && Font.IsLoaded)
                    {
                        var position = (Vector2)Display.DipsToPixels(AbsoluteContentRegion.Location);
                        View.Resources.TextRenderer.Draw(dc.SpriteBatch, textLayoutResult, position, FontColor * dc.Opacity);
                    }
                }

                if (clip != null)
                    dc.PopClipRectangle();
            }
        }

        /// <inheritdoc/>
        protected override void UpdateContent(UltravioletTime time)
        {
            if (contentElement != null)
            {
                contentElement.Update(time);
            }
        }

        /// <summary>
        /// Raises the <see cref="HorizontalContentAlignmentChanged"/> event.
        /// </summary>
        protected virtual void OnHorizontalContentAlignmentChanged()
        {
            var temp = HorizontalContentAlignmentChanged;
            if (temp != null)
            {
                temp(this);
            }
        }

        /// <summary>
        /// Raises the <see cref="VerticalContentAlignmentChanged"/> event.
        /// </summary>
        protected virtual void OnVerticalContentAlignmentChanged()
        {
            var temp = VerticalContentAlignmentChanged;
            if (temp != null)
            {
                temp(this);
            }
        }

        /// <summary>
        /// Raises the <see cref="ContentChanged"/> event.
        /// </summary>
        protected virtual void OnContentChanged()
        {
            var temp = ContentChanged;
            if (temp != null)
            {
                temp(this);
            }
        }

        /// <summary>
        /// Occurs when the value of the <see cref="HorizontalContentAlignment"/> dependency property changes.
        /// </summary>
        /// <param name="dobj">The dependency object that raised the event.</param>
        private static void HandleHorizontalContentAlignmentChanged(DependencyObject dobj)
        {
            var control = (ContentControl)dobj;
            control.OnHorizontalContentAlignmentChanged();
        }

        /// <summary>
        /// Occurs when the value of the <see cref="VerticalContentAlignment"/> dependency property changes.
        /// </summary>
        /// <param name="dobj">The dependency object that raised the event.</param>
        private static void HandleVerticalContentAlignmentChanged(DependencyObject dobj)
        {
            var control = (ContentControl)dobj;
            control.OnVerticalContentAlignmentChanged();
        }

        /// <summary>
        /// Occurs when the value of the <see cref="Content"/> dependency property changes.
        /// </summary>
        /// <param name="dobj">The dependency object that raised the event.</param>
        private static void HandleContentChanged(DependencyObject dobj)
        {
            var control = (ContentControl)dobj;

            var oldElement = control.contentElement;
            if (oldElement != null && oldElement.Parent != null)
                oldElement.Parent.RemoveChild(oldElement);

            control.contentElement = control.Content as UIElement;

            var newElement = control.contentElement;
            if (newElement != null)
                newElement.Parent = control;

            control.UpdateTextParserCache();

            control.OnContentChanged();
        }

        /// <summary>
        /// Updates the cache which contains the element's parsed text.
        /// </summary>
        private void UpdateTextParserCache()
        {
            textParserResult.Clear();

            var content = Content;
            if (content != null && contentElement == null)
            {
                var contentAsString = content.ToString();
                View.Resources.TextRenderer.Parse(contentAsString, textParserResult);
            }

            InvalidateArrange();
        }

        /// <summary>
        /// Updates the cache which contains the element's laid-out text.
        /// </summary>
        /// <param name="availableSize">The amount of space in which the element's text can be laid out.</param>
        private void UpdateTextLayoutCache(Size2D availableSize)
        {
            textLayoutResult.Clear();

            if (textParserResult.Count > 0)
            {
                var availableWidth  = (Int32)Display.DipsToPixels(availableSize.Width);
                var availableHeight = (Int32)Display.DipsToPixels(availableSize.Height);

                var flags    = LayoutUtil.ConvertAlignmentsToTextFlags(HorizontalContentAlignment, VerticalContentAlignment);                
                var settings = new TextLayoutSettings(Font, availableWidth, availableHeight, flags, FontStyle);
                View.Resources.TextRenderer.CalculateLayout(textParserResult, textLayoutResult, settings);
            }
        }

        // State values.
        private UIElement contentElement;

        // Cached parser/layout results for content text.
        private readonly TextParserResult textParserResult = new TextParserResult();
        private readonly TextLayoutResult textLayoutResult = new TextLayoutResult();
    }
}
