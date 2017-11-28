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
using Windows.UI.Xaml.Input;
using Windows.Data.Xml.Dom;

namespace myChat
{
    public sealed partial class MainPage : Page
    {
        // Define a member variable for storing the signed-in user. 
        private MobileServiceUser user;
        private string userUniqueID;
        private bool isLoggedin = false;

        private MobileServiceCollection<ChatItem, ChatItem> items;
        private IMobileServiceTable<ChatItem> chatTable = App.MobileService.GetTable<ChatItem>();

        public PushNotificationChannel PushChannel;

        string lastChatline = "";

        public MainPage()
        {
            this.InitializeComponent();
            
            if (!CheckForInternetAccess())
            {
                string msg1 = "An Internet connection is required for this app and it appears that you are not connected." + Environment.NewLine + Environment.NewLine;
                string msg2 = "Make sure that you have an active Internet connection and try again.";
                UpdateStatus("You are not connected to the Internet", true);

                new MessageDialog(msg1 + msg2, "No Internet").ShowAsync();
            }
            else
            {
                InitNotificationsAsync();
            }
        }

        private async void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            await AuthenticateAsync();
        }

        // Define a method that performs the authentication process
        // using a Facebook sign-in. 
        private async Task AuthenticateAsync()
        {
            prgBusy.IsActive = true;

            userUniqueID = "12345";
            isLoggedin = true;
            await SetUIState(true);

            var message = string.Format("Logged in as {0}", userUniqueID);
            TextUserName.Text = message;

            prgBusy.IsActive = false;

            /*
             * // Define a method that performs the authentication process
        // using a Facebook sign-in. 
        private async Task AuthenticateAsync()
        {
            prgBusy.IsActive = true;
            Exception exception = null;

            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    TextUserName.Text = "Please wait while we log you in...";
                });

                // Sign-in using Facebook authentication.
                user = await App.MobileService.LoginAsync(MobileServiceAuthenticationProvider.Facebook, "publicchat");
                if (user.UserId != null)
                {
                    userUniqueID = user.UserId;
                    isLoggedin = true;
                    SetUIState(true);

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var message = string.Format("Logged in as {0}", userUniqueID);
                        TextUserName.Text = message;
                    });
                }
                else
                {
                    var message = string.Format("Error");
                    TextUserName.Text = message;
                }
               
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                UpdateStatus("Something went wrong when trying to log you in.", true);
                string msg1 = "An error has occurred while trying to sign you in." + Environment.NewLine + Environment.NewLine;
                string msg2 = "Make sure that you have an active Internet connection and try again.";

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    new MessageDialog(msg1 + msg2, "Authentication Error").ShowAsync();
                });
            }
            prgBusy.IsActive = false;
            */

        }


        // Inserts a new chat item to the conversation by posting it in the Azure Mobile 
        // Services table, and posting it in the application's chat window
        private async void InsertChatItem(ChatItem chatItem)
        {
            // This code inserts a new ChatItem into the database
            await chatTable.InsertAsync(chatItem);
        }

        // If we have trouble connecting to cloud services, we need to disable some UI
        private async Task SetUIState(bool isEnabled)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                TextInput.PlaceholderText = (isEnabled) ? "type your message here.." : "please login to chat";
                TextInput.IsEnabled = isEnabled;
                btnWinSend.IsEnabled = isEnabled;
                ListItems.Focus(FocusState.Programmatic);
                RefreshChatItems();
            });
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
                    // Dialog used for debugging 
                    //var dialog = new MessageDialog("Registration successful: " + result.RegistrationId);
                    //dialog.Commands.Add(new UICommand("OK"));
                    //await dialog.ShowAsync();
                    // Update status - inform user that the channel is ready
                    await UpdateStatus("Chat channel is ready.", false);

                    PushChannel = channel;
                    PushChannel.PushNotificationReceived += OnPushNotification;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                await UpdateStatus("Could not initialize cloud services to receive messages.", true);
                string msg1 = "An error has occurred while initializing cloud notifications." + Environment.NewLine + Environment.NewLine;

                string msg2 = "Make sure that you have an active Internet connection and try again.";

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
                RefreshChatItems();
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

                StatusMsg.Foreground = new SolidColorBrush((isError) ? Colors.Red : Colors.Black);
            });
        }

        // Fetches the last chat conversation items from the cloud to be displayed on screen
        private async void RefreshChatItems()
        {
            prgBusy.IsActive = true;

            if (isLoggedin)
            {
                MobileServiceInvalidOperationException exception = null;
                try
                {
                    // The max number of items to retrieve from Azure Mobile Services
                    int n = 20;

                    // refreshes the entries in the list view by querying the ChatItems table.
                    items = await chatTable.OrderByDescending(chatitem => chatitem.TimeStamp).Take(n).ToCollectionAsync();
                    // reverse the order again so the last item is always at the bottom of the list, not the top

                    // Since there cannot be more than 50 items, this is not a bad technique
                    if (items.Count > 0)
                    {
                        if (items.Count < n)
                        {
                            n = items.Count;
                        }

                        for (int i = 0; i < (n - 1); i++)
                        {
                            items.Move(0, n - i - 1);
                        }
                    }

                    ScrollDown();
                }
                catch (MobileServiceInvalidOperationException e)
                {
                    exception = e;
                }

                if (exception != null)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        new MessageDialog(exception.Message, "Error loading items").ShowAsync();
                    });
                }
                else
                {
                    ListItems.ItemsSource = items;
                }
            }
            prgBusy.IsActive = false;
        }

        // Forces the chat window to scroll to the bottom. This uses a special
        // extension method on the ListView control

        private void ScrollDown()
        {
            ListItems.SelectedIndex = ListItems.Items.Count - 1;
            ListItems.ScrollIntoView(ListItems.SelectedItem);
        }

        // Event handler for the refresh app bar button.
        // Useful for scenarios where some notifications might have been lost
        // and the user wants to refresh the screen
        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshChatItems();
        }

        // Event handler for the Send App Bar Button
        private void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            SendChatLine();
        }

        // Event handler for the Send App Bar Button
        private void ButtonSend_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendChatLine();
            }
        }

        // Prepares the chat item to be sent to the cloud
        private void SendChatLine()
        {
            string msg = TextInput.Text.Trim();
            if (isLoggedin && msg.Length > 0)
            {
                var chatItem = new ChatItem { Text = msg, UserName = String.Format("{0}", userUniqueID), TimeStamp = DateTime.UtcNow };
                lastChatline = chatItem.Text;
                InsertChatItem(chatItem);
                TextInput.Text = "";
                RefreshChatItems();
            }
        }

        private async void OnPushNotification(PushNotificationChannel sender, PushNotificationReceivedEventArgs e)
        {
            String notificationContent = String.Empty;

            e.Cancel = true;

            switch (e.NotificationType)
            {
                case PushNotificationType.Badge:
                    notificationContent = e.BadgeNotification.Content.GetXml();
                    break;

                case PushNotificationType.Tile:
                    notificationContent = e.TileNotification.Content.GetXml();
                    break;

                case PushNotificationType.Toast:
                    notificationContent = e.ToastNotification.Content.GetXml();
                    XmlDocument toastXml = e.ToastNotification.Content;

                    // Extract the relevant chat item data from the toast notification payload
                    XmlNodeList toastTextAttributes = toastXml.GetElementsByTagName("text");
                    string username = toastTextAttributes[0].InnerText;
                    string chatline = toastTextAttributes[1].InnerText;
                    string chatdatetime = toastTextAttributes[2].InnerText;

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                    {
                        var chatItem = new ChatItem { Text = chatline, UserName = username };
                        items.Add(chatItem);
                        ScrollDown();
                    });

                    break;

                case PushNotificationType.Raw:
                    notificationContent = e.RawNotification.Content;
                    break;
            }
        }
    }
}
