using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Walker.Service;

namespace Walker.ViewModels
{
    public class SettingsViewViewModel : ViewModelBase
    {
        // private IIsolatedStorageService isolatedStorageService;

        public SettingsViewViewModel()
        {
            // isolatedStorageService = SimpleIoc.Default.GetInstance<IIsolatedStorageService>();
        }

        //public bool IsEnableHistoricalDataDisplayChecked
        //{
        //    get { return isolatedStorageService.BlueToothEnabled; }
        //    set
        //    {
        //        isolatedStorageService.AddOrUpdateValue(
        //            isolatedStorageService._blueToothEnabledSettingKeyName, value);
        //        if (value == true)
        //            Messenger.Default.Send("UpdateHtd");
        //        else
        //            Messenger.Default.Send("NullHtd");
        //    }
        //}
    }
}
