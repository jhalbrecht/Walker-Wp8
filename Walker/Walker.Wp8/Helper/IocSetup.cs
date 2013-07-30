using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Ioc;
using Walker.Pcl.Service;
using Walker.Service;

namespace Walker.Helper
{
    public class IocSetup
    {
        static IocSetup()
        {
            SimpleIoc.Default.Register<INavigationService>(() => new NavigationService());
            SimpleIoc.Default.Register<IWalkerDataService>(() => new WalkerDataService());
            
        }
    }
}
