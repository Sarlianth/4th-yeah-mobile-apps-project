using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace myChat
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            if (!CheckForInternetAccess())
            {
                string msg1 = "An Internet connection is required for this app to work." + Environment.NewLine + Environment.NewLine;
                string msg2 = "Please, make sure that you have an active Internet connection and try again.";
                UpdateStatus("You are not connected to the Internet", true);

                new MessageDialog(msg1 + msg2, "No Internet").ShowAsync();
            }
            else
            {
                UpdateStatus("You are connected to the Internet", false);
            }
        }

        private bool CheckForInternetAccess()
        {
            // This is not the most foolproof way but at least covers basic scenarios
            // like Airplane mode
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task UpdateStatus(string status, bool isError)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StatusMsg.Text = status;

                StatusMsg.Foreground = new SolidColorBrush((isError) ? Colors.Red : Colors.Black);
            });
        }
    }
}
