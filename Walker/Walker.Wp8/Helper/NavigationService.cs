﻿using System;
using Friends.Lib.Helpers;
using Friends.Win8;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Friends.Helpers
{
    public class NavigationService : INavigationService
    {
        public int BackStackDepth
        {
            get
            {
                var frame = ((Frame)Window.Current.Content);
                return frame.BackStackDepth;
            }
        }

        public Type CurrentPageType
        {
            get
            {
                var frame = ((Frame)Window.Current.Content);
                return frame.CurrentSourcePageType;
            }
        }

        public bool CanGoBack
        {
            get
            {
                var frame = ((Frame)Window.Current.Content);
                return frame.CanGoBack;
            }
        }

        public virtual void GoBack()
        {
            var frame = ((Frame)Window.Current.Content);

            if (frame.CanGoBack)
            {
                frame.GoBack();
            }
        }

        public virtual void GoForward()
        {
            var frame = ((Frame)Window.Current.Content);

            if (frame.CanGoForward)
            {
                frame.GoForward();
            }
        }

        public virtual void Navigate(Type sourcePageType)
        {
            ((Frame)Window.Current.Content).Navigate(sourcePageType);
        }

        public virtual void Navigate(Type sourcePageType, object parameter)
        {
            ((Frame)Window.Current.Content).Navigate(sourcePageType, parameter);
        }

        public virtual void GoHome()
        {
            var frame = ((Frame)Window.Current.Content);

            while (frame.CanGoBack)
            {
                frame.GoBack();
            }
        }

        public void Navigate(string pageKey)
        {
            Navigate(pageKey, null);
        }

        public void Navigate(string pageKey, object parameter)
        {
            switch (pageKey)
            {
                case "DetailsPage":
                    Navigate(typeof(DetailsPage), parameter);
                    break;
            }
        }
    }
}