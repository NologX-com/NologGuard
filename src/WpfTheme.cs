using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Pen = System.Drawing.Pen;

namespace NologGuard
{
    public static class WpfTheme
    {
        public static readonly Color BgWindowColor = Color.FromRgb(10, 15, 28);
        public static readonly Color BgCardColor = Color.FromRgb(17, 25, 44);
        public static readonly Color BgCardAltColor = Color.FromRgb(25, 36, 61);
        public static readonly Color BgInputColor = Color.FromRgb(11, 18, 34);
        public static readonly Color BorderColor = Color.FromRgb(38, 52, 80);
        public static readonly Color BorderHoverColor = Color.FromRgb(56, 189, 248);

        public static readonly Color AccentGreenColor = Color.FromRgb(16, 185, 129);
        public static readonly Color AccentGreenDarkColor = Color.FromRgb(6, 95, 70);
        public static readonly Color AccentBlueColor = Color.FromRgb(37, 99, 235);
        public static readonly Color AccentCyanColor = Color.FromRgb(56, 189, 248);
        public static readonly Color AccentRoseColor = Color.FromRgb(251, 113, 133);
        public static readonly Color AccentAmberColor = Color.FromRgb(245, 158, 11);
        public static readonly Color AccentRedColor = Color.FromRgb(220, 38, 38);

        public static readonly Color TextPrimaryColor = Color.FromRgb(248, 250, 252);
        public static readonly Color TextSecondaryColor = Color.FromRgb(148, 163, 184);
        public static readonly Color TextMutedColor = Color.FromRgb(100, 116, 139);

        public static readonly Brush BgWindow = Freeze(new SolidColorBrush(BgWindowColor));
        public static readonly Brush BgCard = Freeze(new SolidColorBrush(BgCardColor));
        public static readonly Brush BgCardAlt = Freeze(new SolidColorBrush(BgCardAltColor));
        public static readonly Brush BgInput = Freeze(new SolidColorBrush(BgInputColor));
        public static readonly Brush BorderSubtle = Freeze(new SolidColorBrush(BorderColor));
        public static readonly Brush BorderCyan = Freeze(new SolidColorBrush(BorderHoverColor));

        public static readonly Brush AccentGreen = Freeze(new SolidColorBrush(AccentGreenColor));
        public static readonly Brush AccentGreenDark = Freeze(new SolidColorBrush(AccentGreenDarkColor));
        public static readonly Brush AccentBlue = Freeze(new SolidColorBrush(AccentBlueColor));
        public static readonly Brush AccentCyan = Freeze(new SolidColorBrush(AccentCyanColor));
        public static readonly Brush AccentRose = Freeze(new SolidColorBrush(AccentRoseColor));
        public static readonly Brush AccentAmber = Freeze(new SolidColorBrush(AccentAmberColor));
        public static readonly Brush AccentRed = Freeze(new SolidColorBrush(AccentRedColor));

        public static readonly Brush TextPrimary = Freeze(new SolidColorBrush(TextPrimaryColor));
        public static readonly Brush TextSecondary = Freeze(new SolidColorBrush(TextSecondaryColor));
        public static readonly Brush TextMuted = Freeze(new SolidColorBrush(TextMutedColor));

        public static readonly Brush PowerOnBg = Freeze(new System.Windows.Media.LinearGradientBrush(
            Color.FromRgb(16, 185, 129),
            Color.FromRgb(5, 150, 105),
            90.0));
        public static readonly Brush PowerOnBorder = Freeze(new SolidColorBrush(Color.FromRgb(110, 231, 183)));
        public static readonly Brush PowerOffBg = Freeze(new System.Windows.Media.LinearGradientBrush(
            Color.FromRgb(37, 99, 235),
            Color.FromRgb(29, 78, 216),
            90.0));
        public static readonly Brush PowerOffBorder = Freeze(new SolidColorBrush(Color.FromRgb(96, 165, 250)));

        public static readonly FontFamily UiFont = new FontFamily(
            "Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI, Yu Gothic UI, Malgun Gothic, Leelawadee UI, Nirmala UI, Microsoft Sans Serif");
        public static readonly FontFamily MonoFont = new FontFamily("Cascadia Mono, Consolas, Courier New");

        private static Brush Freeze(Brush br)
        {
            br.Freeze();
            return br;
        }

        public static Style CreateFlatButtonStyle(Brush bg, Brush hoverBg, CornerRadius radius)
        {
            Style style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimary));
            style.Setters.Add(new Setter(Control.BackgroundProperty, bg));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, BorderSubtle));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.FontFamilyProperty, UiFont));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 6, 12, 6)));

            ControlTemplate tpl = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "bd";
            border.SetValue(Border.CornerRadiusProperty, radius);
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            tpl.VisualTree = border;

            Trigger hoverTrig = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrig.Setters.Add(new Setter(Border.BackgroundProperty, hoverBg, "bd"));
            tpl.Triggers.Add(hoverTrig);

            Trigger disabledTrig = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrig.Setters.Add(new Setter(UIElement.OpacityProperty, 0.55));
            tpl.Triggers.Add(disabledTrig);

            style.Setters.Add(new Setter(Control.TemplateProperty, tpl));
            return style;
        }

        public static Style CreateToggleSwitchStyle()
        {
            Style style = new Style(typeof(CheckBox));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
            style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));

            ControlTemplate tpl = new ControlTemplate(typeof(CheckBox));
            FrameworkElementFactory track = new FrameworkElementFactory(typeof(Border));
            track.Name = "track";
            track.SetValue(FrameworkElement.WidthProperty, 42.0);
            track.SetValue(FrameworkElement.HeightProperty, 22.0);
            track.SetValue(Border.CornerRadiusProperty, new CornerRadius(11));
            track.SetValue(Border.BackgroundProperty, BgCardAlt);
            track.SetValue(Border.BorderBrushProperty, BorderSubtle);
            track.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            FrameworkElementFactory thumb = new FrameworkElementFactory(typeof(Ellipse));
            thumb.Name = "thumb";
            thumb.SetValue(FrameworkElement.WidthProperty, 16.0);
            thumb.SetValue(FrameworkElement.HeightProperty, 16.0);
            thumb.SetValue(Shape.FillProperty, TextSecondary);
            thumb.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            thumb.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            thumb.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 0, 2, 0));

            track.AppendChild(thumb);
            tpl.VisualTree = track;

            Trigger checkedTrig = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
            checkedTrig.Setters.Add(new Setter(Border.BackgroundProperty, AccentGreen, "track"));
            checkedTrig.Setters.Add(new Setter(Border.BorderBrushProperty, AccentGreen, "track"));
            checkedTrig.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right, "thumb"));
            checkedTrig.Setters.Add(new Setter(Shape.FillProperty, System.Windows.Media.Brushes.White, "thumb"));
            tpl.Triggers.Add(checkedTrig);

            style.Setters.Add(new Setter(Control.TemplateProperty, tpl));
            return style;
        }

        public static Style CreateDarkComboBoxStyle()
        {
            Style style = new Style(typeof(ComboBox));
            style.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimary));
            style.Setters.Add(new Setter(Control.BackgroundProperty, BgInput));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, BorderSubtle));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.FontFamilyProperty, UiFont));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 12.5));
            style.Setters.Add(new Setter(FrameworkElement.HeightProperty, 32.0));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));

            // Item style
            Style itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimary));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, BgCard));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 7, 10, 7)));
            ControlTemplate itemTpl = new ControlTemplate(typeof(ComboBoxItem));
            FrameworkElementFactory itemBd = new FrameworkElementFactory(typeof(Border));
            itemBd.Name = "ibd";
            itemBd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            itemBd.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            FrameworkElementFactory itemCp = new FrameworkElementFactory(typeof(ContentPresenter));
            itemBd.AppendChild(itemCp);
            itemTpl.VisualTree = itemBd;
            Trigger itemHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            itemHover.Setters.Add(new Setter(Border.BackgroundProperty, BgCardAlt, "ibd"));
            itemTpl.Triggers.Add(itemHover);
            Trigger itemSel = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            itemSel.Setters.Add(new Setter(Border.BackgroundProperty, AccentGreenDark, "ibd"));
            itemTpl.Triggers.Add(itemSel);
            itemStyle.Setters.Add(new Setter(Control.TemplateProperty, itemTpl));
            style.Setters.Add(new Setter(ItemsControl.ItemContainerStyleProperty, itemStyle));

            // Main ComboBox template
            ControlTemplate tpl = new ControlTemplate(typeof(ComboBox));
            FrameworkElementFactory root = new FrameworkElementFactory(typeof(Grid));

            FrameworkElementFactory toggle = new FrameworkElementFactory(typeof(ToggleButton));
            toggle.Name = "ToggleButton";
            toggle.SetValue(Control.FocusableProperty, false);
            toggle.SetValue(ToggleButton.ClickModeProperty, ClickMode.Press);
            Binding isDropDownBinding = new Binding("IsDropDownOpen")
            {
                Mode = BindingMode.TwoWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            };
            toggle.SetBinding(ToggleButton.IsCheckedProperty, isDropDownBinding);

            ControlTemplate toggleTpl = new ControlTemplate(typeof(ToggleButton));
            FrameworkElementFactory box = new FrameworkElementFactory(typeof(Border));
            box.Name = "box";
            box.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            box.SetValue(Border.BackgroundProperty, BgInput);
            box.SetValue(Border.BorderBrushProperty, BorderSubtle);
            box.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            FrameworkElementFactory tGrid = new FrameworkElementFactory(typeof(Grid));
            FrameworkElementFactory col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            FrameworkElementFactory col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(28));
            tGrid.AppendChild(col1);
            tGrid.AppendChild(col2);

            FrameworkElementFactory arrow = new FrameworkElementFactory(typeof(Path));
            arrow.SetValue(Grid.ColumnProperty, 1);
            arrow.SetValue(Path.DataProperty, Geometry.Parse("M 0,0 L 4.5,4.5 L 9,0"));
            arrow.SetValue(Shape.StrokeProperty, TextSecondary);
            arrow.SetValue(Shape.StrokeThicknessProperty, 1.6);
            arrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            tGrid.AppendChild(arrow);
            box.AppendChild(tGrid);
            toggleTpl.VisualTree = box;
            Trigger boxHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            boxHover.Setters.Add(new Setter(Border.BorderBrushProperty, BorderCyan, "box"));
            toggleTpl.Triggers.Add(boxHover);
            toggle.SetValue(Control.TemplateProperty, toggleTpl);

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(UIElement.IsHitTestVisibleProperty, false);
            presenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ComboBox.SelectionBoxItemProperty));
            presenter.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ComboBox.SelectionBoxItemTemplateProperty));
            presenter.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 30, 0));
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            FrameworkElementFactory popup = new FrameworkElementFactory(typeof(Popup));
            popup.Name = "Popup";
            popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
            popup.SetValue(Popup.IsOpenProperty, new TemplateBindingExtension(ComboBox.IsDropDownOpenProperty));
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetValue(UIElement.FocusableProperty, false);
            popup.SetValue(Popup.PopupAnimationProperty, PopupAnimation.Fade);

            FrameworkElementFactory dropBorder = new FrameworkElementFactory(typeof(Border));
            dropBorder.SetValue(Border.BackgroundProperty, BgCard);
            dropBorder.SetValue(Border.BorderBrushProperty, BorderSubtle);
            dropBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            dropBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            dropBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 3, 0, 0));
            dropBorder.SetValue(FrameworkElement.MinWidthProperty, new TemplateBindingExtension(FrameworkElement.ActualWidthProperty));
            dropBorder.SetValue(FrameworkElement.MaxHeightProperty, 240.0);

            FrameworkElementFactory scroll = new FrameworkElementFactory(typeof(ScrollViewer));
            scroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            FrameworkElementFactory itemsHost = new FrameworkElementFactory(typeof(ItemsPresenter));
            scroll.AppendChild(itemsHost);
            dropBorder.AppendChild(scroll);
            popup.AppendChild(dropBorder);

            root.AppendChild(toggle);
            root.AppendChild(presenter);
            root.AppendChild(popup);
            tpl.VisualTree = root;

            style.Setters.Add(new Setter(Control.TemplateProperty, tpl));
            return style;
        }

        public static Style CreateDarkTextBoxStyle(bool monospace)
        {
            Style style = new Style(typeof(TextBox));
            style.Setters.Add(new Setter(Control.BackgroundProperty, BgInput));
            style.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimary));
            style.Setters.Add(new Setter(TextBoxBase.CaretBrushProperty, AccentCyan));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, BorderSubtle));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.FontFamilyProperty, monospace ? MonoFont : UiFont));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 4, 8, 4)));

            ControlTemplate tpl = new ControlTemplate(typeof(TextBox));
            FrameworkElementFactory bd = new FrameworkElementFactory(typeof(Border));
            bd.Name = "bd";
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            bd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            bd.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));

            FrameworkElementFactory sv = new FrameworkElementFactory(typeof(ScrollViewer));
            sv.Name = "PART_ContentHost";
            sv.SetValue(FrameworkElement.MarginProperty, new Thickness(1));
            bd.AppendChild(sv);
            tpl.VisualTree = bd;

            Trigger focusTrig = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true };
            focusTrig.Setters.Add(new Setter(Border.BorderBrushProperty, BorderCyan, "bd"));
            tpl.Triggers.Add(focusTrig);

            style.Setters.Add(new Setter(Control.TemplateProperty, tpl));
            return style;
        }

        public static Style CreateDarkScrollBarStyle()
        {
            string xaml =
                "<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                "       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ScrollBar'>" +
                "  <Setter Property='Background' Value='Transparent'/>" +
                "  <Setter Property='Width' Value='8'/>" +
                "  <Setter Property='MinWidth' Value='8'/>" +
                "  <Setter Property='Template'>" +
                "    <Setter.Value>" +
                "      <ControlTemplate TargetType='ScrollBar'>" +
                "        <Grid Background='Transparent'>" +
                "          <Track Name='PART_Track' IsDirectionReversed='True'>" +
                "            <Track.Thumb>" +
                "              <Thumb>" +
                "                <Thumb.Template>" +
                "                  <ControlTemplate TargetType='Thumb'>" +
                "                    <Border x:Name='tbd' CornerRadius='4' Margin='1' Background='#263450'/>" +
                "                    <ControlTemplate.Triggers>" +
                "                      <Trigger Property='IsMouseOver' Value='True'>" +
                "                        <Setter TargetName='tbd' Property='Background' Value='#64748B'/>" +
                "                      </Trigger>" +
                "                    </ControlTemplate.Triggers>" +
                "                  </ControlTemplate>" +
                "                </Thumb.Template>" +
                "              </Thumb>" +
                "            </Track.Thumb>" +
                "          </Track>" +
                "        </Grid>" +
                "      </ControlTemplate>" +
                "    </Setter.Value>" +
                "  </Setter>" +
                "  <Style.Triggers>" +
                "    <Trigger Property='Orientation' Value='Horizontal'>" +
                "      <Setter Property='Width' Value='Auto'/>" +
                "      <Setter Property='MinWidth' Value='0'/>" +
                "      <Setter Property='Height' Value='8'/>" +
                "      <Setter Property='MinHeight' Value='8'/>" +
                "    </Trigger>" +
                "  </Style.Triggers>" +
                "</Style>";
            return (Style)System.Windows.Markup.XamlReader.Parse(xaml);
        }

        public static Icon CreateShieldIcon(bool active)
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);

                System.Drawing.Color fill = active
                    ? System.Drawing.Color.FromArgb(16, 185, 129)
                    : System.Drawing.Color.FromArgb(71, 85, 105);
                System.Drawing.Color rim = active
                    ? System.Drawing.Color.FromArgb(110, 231, 183)
                    : System.Drawing.Color.FromArgb(148, 163, 184);

                PointF[] shield = new PointF[]
                {
                    new PointF(16f, 2.5f),
                    new PointF(28f, 7f),
                    new PointF(26.5f, 18f),
                    new PointF(16f, 29.5f),
                    new PointF(5.5f, 18f),
                    new PointF(4f, 7f)
                };

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddPolygon(shield);
                    using (SolidBrush br = new SolidBrush(fill))
                    {
                        g.FillPath(br, gp);
                    }
                    using (Pen pen = new Pen(rim, 1.8f))
                    {
                        g.DrawPath(pen, gp);
                    }
                }

                using (Pen glyph = new Pen(System.Drawing.Color.White, 2.2f))
                {
                    glyph.StartCap = LineCap.Round;
                    glyph.EndCap = LineCap.Round;
                    if (active)
                    {
                        g.DrawLine(glyph, 11f, 16f, 14.5f, 19.5f);
                        g.DrawLine(glyph, 14.5f, 19.5f, 21.5f, 11.5f);
                    }
                    else
                    {
                        g.DrawLine(glyph, 16f, 10f, 16f, 16f);
                        g.DrawArc(glyph, 11f, 11f, 10f, 10f, -45f, 270f);
                    }
                }

                IntPtr hIcon = bmp.GetHicon();
                try
                {
                    using (Icon temp = Icon.FromHandle(hIcon))
                    {
                        return (Icon)temp.Clone();
                    }
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
