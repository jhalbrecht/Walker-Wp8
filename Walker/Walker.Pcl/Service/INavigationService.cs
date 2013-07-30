namespace Walker.Pcl.Service
{
    public interface INavigationService
    {
        void GoBack();
        void Navigate(string pageKey);
        void Navigate(string pageKey, object parameter);
    }
}