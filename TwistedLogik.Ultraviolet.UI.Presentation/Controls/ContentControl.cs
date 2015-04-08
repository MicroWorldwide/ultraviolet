﻿using System;
using System.ComponentModel;
using TwistedLogik.Ultraviolet.Graphics.Graphics2D;
using TwistedLogik.Ultraviolet.UI.Presentation.Documents;
using TwistedLogik.Ultraviolet.UI.Presentation.Styles;

namespace TwistedLogik.Ultraviolet.UI.Presentation.Controls
{
    /// <summary>
    /// Represents a control which displays a single item of content.
    /// </summary>
    [DefaultProperty("Content")]
    public abstract class ContentControl : Control, IItemContainer, ITextHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentControl"/> class.
        /// </summary>
        /// <param name="uv">The Ultraviolet context.</param>
        /// <param name="name">The element's identifying name within its namescope.</param>
        public ContentControl(UltravioletContext uv, String name)
            : base(uv, name)
        {

        }

        /// <inheritdoc/>
        void IItemContainer.PrepareItemContainer(Object item)
        {
            treatContentAsLogicalChild = false;
            Content = item;
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
        /// Gets a value indicating whether the control has any content.
        /// </summary>
        public Boolean HasContent
        {
            get { return GetValue<Boolean>(HasContentProperty); }
        }

        /// <summary>
        /// Gets or sets the font used to draw the control's text.
        /// </summary>
        public SourcedResource<SpriteFont> Font
        {
            get { return GetValue<SourcedResource<SpriteFont>>(FontProperty); }
            set { SetValue<SourcedResource<SpriteFont>>(FontProperty, value); }
        }

        /// <summary>
        /// Gets or sets the color used to draw the control's text.
        /// </summary>
        public Color FontColor
        {
            get { return GetValue<Color>(FontColorProperty); }
            set { SetValue<Color>(FontColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font style which is used to draw the control's text.
        /// </summary>
        public SpriteFontStyle FontStyle
        {
            get { return GetValue<SpriteFontStyle>(FontStyleProperty); }
            set { SetValue<SpriteFontStyle>(FontStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Content"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(Object), typeof(ContentControl),
            new PropertyMetadata<Object>(null, PropertyMetadataOptions.AffectsMeasure | PropertyMetadataOptions.CoerceObjectToString, HandleContentChanged));

        /// <summary>
        /// The private access key for the <see cref="HasContent"/> read-only dependency property.
        /// </summary>
        private static readonly DependencyPropertyKey HasContentPropertyKey = DependencyProperty.RegisterReadOnly("HasContent", typeof(Boolean), typeof(ContentControl),
            new PropertyMetadata<Boolean>());

        /// <summary>
        /// Identifies the <see cref="HasContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasContentProperty = HasContentPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="Font"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontProperty = TextElement.FontProperty.AddOwner(typeof(ContentControl), 
            new PropertyMetadata<SourcedResource<SpriteFont>>(HandleFontChanged));

        /// <summary>
        /// Identifies the <see cref="FontColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontColorProperty = TextElement.FontColorProperty.AddOwner(typeof(ContentControl));

        /// <summary>
        /// Identifies the <see cref="FontStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(typeof(ContentControl));

        /// <summary>
        /// Gets a value indicating whether the content control treats its content as a logical child.
        /// </summary>
        internal Boolean TreatContentAsLogicalChild
        {
            get { return treatContentAsLogicalChild; }
        }

        /// <summary>
        /// Gets the control's content presenter.
        /// </summary>
        internal ContentPresenter ContentPresenter
        {
            get { return contentPresenter; }
            set { contentPresenter = value; }
        }

        /// <inheritdoc/>
        protected internal override void RemoveLogicalChild(UIElement child)
        {
            if (TreatContentAsLogicalChild && Content == child)
            {
                Content = null;
            }
            base.RemoveLogicalChild(child);
        }

        /// <inheritdoc/>
        protected internal override UIElement GetLogicalChild(Int32 childIndex)
        {
            if (TreatContentAsLogicalChild && contentElement != null)
            {
                if (childIndex == 0)
                {
                    return contentElement;
                }
                return base.GetLogicalChild(childIndex - 1);
            }
            return base.GetLogicalChild(childIndex);
        }

        /// <inheritdoc/>
        protected internal override UIElement GetVisualChild(Int32 childIndex)
        {
            return base.GetVisualChild(childIndex);
        }

        /// <inheritdoc/>
        protected internal override Int32 LogicalChildrenCount
        {
            get { return (TreatContentAsLogicalChild && contentElement != null ? 1 : 0) + base.LogicalChildrenCount; }
        }

        /// <inheritdoc/>
        protected internal override Int32 VisualChildrenCount
        {
            get { return base.VisualChildrenCount; }
        }
        
        /// <inheritdoc/>
        protected override void ReloadContentCore(Boolean recursive)
        {
            ReloadFont();

            base.ReloadContentCore(recursive);
        }

        /// <summary>
        /// Raises the <see cref="ContentChanged"/> event.
        /// </summary>
        protected virtual void OnContentChanged()
        {

        }

        /// <summary>
        /// Reloads the <see cref="Font"/> resource.
        /// </summary>
        protected void ReloadFont()
        {
            LoadResource(Font);
        }

        /// <summary>
        /// Occurs when the value of the <see cref="Content"/> dependency property changes.
        /// </summary>
        private static void HandleContentChanged(DependencyObject dobj, Object oldValue, Object newValue)
        {
            var control = (ContentControl)dobj;

            var oldElement = control.contentElement;
            if (oldElement != null)
            {
                if (control.TreatContentAsLogicalChild)
                {
                    oldElement.ChangeLogicalParent(null);
                }
                oldElement.ChangeVisualParent(null);
            }

            control.contentElement = control.Content as UIElement;
            control.SetValue<Boolean>(HasContentPropertyKey, control.Content != null);

            var newElement = control.contentElement;
            if (newElement != null)
            {
                if (control.TreatContentAsLogicalChild)
                {
                    newElement.ChangeLogicalParent(control);
                }
                newElement.ChangeVisualParent(control.ContentPresenter);
            }

            if (control.contentPresenter != null)
                control.contentPresenter.InvalidateMeasure();

            control.OnContentChanged();
        }

        /// <summary>
        /// Occurs when the value of the <see cref="Font"/> dependency property changes.
        /// </summary>
        private static void HandleFontChanged(DependencyObject dobj, SourcedResource<SpriteFont> oldValue, SourcedResource<SpriteFont> newValue)
        {
            ((ContentControl)dobj).ReloadFont();
        }

        // State values.
        private UIElement contentElement;
        private ContentPresenter contentPresenter;
        private Boolean treatContentAsLogicalChild = true;
    }
}
