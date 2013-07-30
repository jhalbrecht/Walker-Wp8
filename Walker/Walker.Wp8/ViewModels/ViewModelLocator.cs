using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using Walker.Pcl.ViewModel;

namespace Walker.ViewModels
{
    public class ViewModelLocator
    {

        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            SimpleIoc.Default.Register<Wp8AppHubViewModel>();
            SimpleIoc.Default.Register<SensorsViewModel>();
        }

        /// <summary>
        /// Gets the ViewModelPropertyName property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public Wp8AppHubViewModel Wp8AppHub
        {
            get
            {
                return ServiceLocator.Current.GetInstance<Wp8AppHubViewModel>();
            }
        }

        /// <summary>
        /// Gets the SettingsViewViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public SettingsViewViewModel Settings
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SettingsViewViewModel>();
            }
        }

        /// <summary>
        /// Gets the SensorsViewModel property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public SensorsViewModel Sensors
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SensorsViewModel>();
            }
        }
    }
}
