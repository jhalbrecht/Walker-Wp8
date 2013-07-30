using System;
using System.Windows;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Controls;
using Walker.Pcl.Service;

namespace Walker.Service
{
    public class NavigationService : INavigationService
    {
        private PhoneApplicationFrame _mainFrame;

        public event NavigatingCancelEventHandler Navigating;

        public void GoBack()
        {
            if (EnsureMainFrame()
                && _mainFrame.CanGoBack)
            {
                _mainFrame.GoBack();
            }
        }

        public void NavigateTo(Uri pageUri)
        {
            if (EnsureMainFrame())
            {
                _mainFrame.Navigate(pageUri);
            }
        }

        private bool EnsureMainFrame()
        {
            if (_mainFrame != null)
            {
                return true;
            }

            _mainFrame = Application.Current.RootVisual as PhoneApplicationFrame;

            if (_mainFrame != null)
            {
                // Could be null if the app runs inside a design tool
                _mainFrame.Navigating += (s, e) =>
                {
                    if (Navigating != null)
                    {
                        Navigating(s, e);
                    }
                };

                return true;
            }

            return false;
        }

        public void Navigate(string pageKey)
        {
            NavigateTo(new Uri(string.Format("/{0}.xaml", pageKey), UriKind.Relative));
        }

        public void Navigate(string pageKey, object parameter)
        {
            var key = Guid.NewGuid().ToString();
            SimpleIoc.Default.Register<object>(() => parameter, key);

            NavigateTo(new Uri(string.Format("/{0}.xaml?key={1}", pageKey, key), UriKind.Relative));
        }
    }
}