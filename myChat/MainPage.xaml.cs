using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.WindowsAzure.Messaging;

namespace myChat
{
    public sealed partial class MainPage : Page
    {
        // Define a member variable for storing the signed-in user. 
        private MobileServiceUser user;
        private string userUniqueID;

        private MobileServiceCollection<ChatItem, ChatItem> items;
        private IMobileServiceTable<ChatItem> chatTable = App.MobileService.GetTable<ChatItem>();

        public PushNotificationChannel PushChannel;

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
                // Channel URI is registered in notification hub
                InitNotificationsAsync();
            }



#if WINDOWS_APP
            btnWinSend.Visibility = Windows.UI.Xaml.Visibility.Visible;
            TextInput.Height = btnWinSend.Height;
#endif
        }

        private async void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            // Login the user and then load data from the mobile app.
            if (await AuthenticateAsync())
            {
                // Switch the buttons and load items from the mobile app.
                myButton.Visibility = Visibility.Collapsed;
            }
        }

        // Define a method that performs the authentication process
        // using a Facebook sign-in. 
        private async System.Threading.Tasks.Task<bool> AuthenticateAsync()
        {
            string message;
            bool success = false;
            try
            {
                // Sign-in using Facebook authentication.
                user = await App.MobileService
                    .LoginAsync(MobileServiceAuthenticationProvider.Facebook, "publicchat");
                message =
                    string.Format("You are now signed in - {0}", user.UserId);

                userUniqueID = user.UserId;

                success = true;
            }
            catch (InvalidOperationException)
            {
                message = "You must log in. Login Required";
            }

            var dialog = new MessageDialog(message);
            dialog.Commands.Add(new UICommand("OK"));
            await dialog.ShowAsync();
            return success;
        }


        /* Adapted from https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-windows-store-dotnet-get-started-wns-push-notification */
        private async void InitNotificationsAsync()
        {
            Exception exception = null;

            try
            {
                var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();

                var hub = new NotificationHub(ConfigSecrets.AzureNotificationHubName, ConfigSecrets.AzureNotificationHubCnxString);
                var result = await hub.RegisterNativeAsync(channel.Uri);

                // Display id to confirm its successfull
                if (result.RegistrationId != null)
                {
                    var dialog = new MessageDialog("Registration successful: " + result.RegistrationId);
                    dialog.Commands.Add(new UICommand("OK"));
                    await dialog.ShowAsync();
                    // Update status - inform user that the channel is ready
                    UpdateStatus("Chat channel is ready.", false);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                UpdateStatus("Could not initialize cloud services to receive messages.", true);
                string msg1 = "An error has occurred while initializing cloud notifications." + Environment.NewLine + Environment.NewLine;

                string msg2 = "Make sure that you have an active Internet connection and try again.";

                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    new MessageDialog(msg1 + msg2, "Initialization Error").ShowAsync();
                });
            }
        }

        // Method checkning if device is connected to internet
        private bool CheckForInternetAccess()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Method to update textblock with status information in it
        private async Task UpdateStatus(string status, bool isError)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StatusMsg.Text = status;

                // If error, change foreground to red, else black
                StatusMsg.Foreground = new SolidColorBrush((isError) ? Colors.Red : Colors.Black);
            });
        }
    }
}
