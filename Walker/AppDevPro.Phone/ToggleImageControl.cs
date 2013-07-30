using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

// http://www.geekchamp.com/articles/creating-a-wp7-custom-control-in-7-steps

namespace AppDevPro.Phone
{

    [TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
    [TemplateVisualState(Name = "Checked", GroupName = "CommonStates")]
    [TemplatePart(Name = "CheckBox", Type = typeof(CheckBox))]
    public class ToggleImageControl : ContentControl
    {
        private const string CheckedStates = "Checked";
        private const string UnCheckedState = "UnChecked";
        private CheckBox checkBox;

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked",
            typeof(bool),
            typeof(ToggleImageControl),
            new PropertyMetadata(false, new PropertyChangedCallback(ToggleImageControl.OnIsCheckedPropertyChanged)));

        public static readonly DependencyProperty IconSourceProperty =
          DependencyProperty.Register("IconSource", typeof(ImageSource), typeof(ToggleImageControl), null);

        public ToggleImageControl()
        {
            DefaultStyleKey = typeof(ToggleImageControl);

        }

        public bool IsChecked
        {
            get { return (bool)base.GetValue(IsCheckedProperty); }
            set
            { base.SetValue(IsCheckedProperty, value); }
        }

        public ImageSource IconSource
        {
            get { return base.GetValue(IconSourceProperty) as ImageSource; }
            set { base.SetValue(IconSourceProperty, value); }
        }

        // Events
        public event RoutedEventHandler Checked;
        internal virtual void OnChecked(RoutedEventArgs e)
        {
            this.ChangeVisualState(false);
            RoutedEventHandler handler = this.Checked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event RoutedEventHandler Unchecked;
        internal virtual void OnUnchecked(RoutedEventArgs e)
        {
            this.ChangeVisualState(false);
            RoutedEventHandler handler = this.Unchecked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (checkBox != null)
            {
                checkBox.Click -= OnCheckBoxClick;
            }

            checkBox = this.GetTemplateChild("CheckBox") as CheckBox;

            // Attach to the Click event
            if (checkBox != null)
            {
                checkBox.Click += OnCheckBoxClick;
            }

            this.ChangeVisualState(false);
        }

        private static void OnIsCheckedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ToggleImageControl button = d as ToggleImageControl;
            bool newValue = (bool)e.NewValue;
            //button.ChangeVisualState(false);

            if (newValue == true)
            {
                button.OnChecked(new RoutedEventArgs());
            }
            else if (newValue == false)
            {
                button.OnUnchecked(new RoutedEventArgs());
            }
        }

        protected internal virtual void OnToggle()
        {
            bool isChecked = this.IsChecked;
            if (isChecked == true)
            {
                this.IsChecked = false;
            }
            else
            {
                this.IsChecked = true;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            this.OnToggle();
            base.OnMouseLeftButtonDown(e);
            this.ChangeVisualState(false);
        }

        private void OnCheckBoxClick(object sender, RoutedEventArgs e)
        {
            if (this.IsChecked)
            {
                this.IsChecked = false;
                this.checkBox.IsChecked = true;
            }
        }

        private void ChangeVisualState(bool useTransitions)
        {
            if (!IsChecked)
            {
                VisualStateManager.GoToState(this, UnCheckedState, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, CheckedStates, useTransitions);
            }
        }


    }
}
