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
using Windows.Data.Xml.Dom;
using winsdkfb;
using winsdkfb.Graph;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Http;

namespace myChat
{
    public sealed partial class MainPage : Page
    {
        private string userUniqueID;
        private bool isLoggedin = false;
        int lastCount = 0;
        private MobileServiceCollection<ChatItem, ChatItem> items;
        private IMobileServiceTable<ChatItem> chatTable = App.MobileService.GetTable<ChatItem>();

        private MobileServiceCollection<OnlineUsers, OnlineUsers> users;
        private IMobileServiceTable<OnlineUsers> usersTable = App.MobileService.GetTable<OnlineUsers>();

        public PushNotificationChannel PushChannel;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private static Random rnd = new Random();
        private int randomID = rnd.Next();


        public MainPage()
        {
            this.InitializeComponent();
            
            if (!CheckForInternetAccess())
            {
                UpdateStatus("You are not connected to the Internet", true);
            }
            else
            {
                UpdateStatus("You are successfully connected to the Internet", false);
                InitNotificationsAsync();
                //Prompt user to login
                prompt();
            }
        }

        public async void prompt()
        {
            await AuthenticateAsync();
        }
        public void DispatcherTimerSetup()
        {
            RefreshChatItems();
            dispatcherTimer.Tick += dispatcherTimer_TickAsync;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private async void dispatcherTimer_TickAsync(object sender, object e)
        {
            if (isLoggedin)
            {
                // The max number of items to retrieve from Azure Mobile Services
                int n = 50;

                items = await chatTable.Take(n).ToCollectionAsync();
                 // refreshes the entries in the list view by querying the ChatItems table.

                if (lastCount == items.Count)
                {
                    //UpdateStatus("Messages: "+items.Count, false);
                }
                else if (items.Count >= 49)
                {
                    dispatcherTimer.Stop();
                    ListItems.IsEnabled = false;
                    TextInput.IsEnabled = false;
                    prgBusy.IsActive = true;
                    var dbReviewItems = await chatTable.ToListAsync();
                    foreach (var item in dbReviewItems)
                    {
                        await chatTable.DeleteAsync(item);
                    }

                    ListItems.IsEnabled = true;
                    TextInput.IsEnabled = true;
                    prgBusy.IsActive = false;
                    dispatcherTimer.Start();
                }
                else
                {
                    RefreshChatItems();
                }
            }
        }

        public async Task deleteEntitiesAsync()
        {
            dispatcherTimer.Stop();
            var dbReviewItems = await chatTable.ToListAsync();
            foreach (var item in dbReviewItems)
                await chatTable.DeleteAsync(item);
            dispatcherTimer.Start();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await AuthenticateAsync();
        }

        // Define a method that performs the authentication process
        // using a Facebook sign-in. 
        private async Task AuthenticateAsync()
        {
            prgBusy.IsActive = true;
            FBSession sess = FBSession.ActiveSession;
            sess.FBAppId = ConfigSecrets.FacebookAppID;
            sess.WinAppId = ConfigSecrets.WindowsStoreID;

            List<String> permissionList = new List<String>();
            permissionList.Add("public_profile");
            FBPermissions permissions = new FBPermissions(permissionList);

            // Login to Facebook
            FBResult result = await sess.LoginAsync(permissions);

            FBUser user = sess.User;

            if (result.Succeeded)
            {
                if (sess.LoggedIn)
                {
                    userUniqueID = user.Name;
                    isLoggedin = true;
                    await SetUIState(true);

                    var message = string.Format("Logged in as {0}", userUniqueID);
                    TextUserName.Text = message;

                    insertUser(userUniqueID);
                    
                    DispatcherTimerSetup();                  
                }
                else
                {
                    await UpdateStatus("Please login to chat with others", true);
                }
            }
            else
            {
                await UpdateStatus("Please login to chat with others", false);
            }
            prgBusy.IsActive = false;
        }

        async void Logout()
        {
            FBSession sess = FBSession.ActiveSession;
            await sess.LogoutAsync();

            var onlineUser = new OnlineUsers { Id = "user_" + userUniqueID + "_id_" + randomID, username = String.Format("{0}", userUniqueID) };
            await usersTable.DeleteAsync(onlineUser);

            await SetUIState(false);
        }

        async void insertUser(string name)
        {
            var onlineUser = new OnlineUsers { Id = "user_" + userUniqueID +"_id_" + randomID, username = String.Format("{0}", name) };
            await usersTable.InsertAsync(onlineUser);
            var onlineUsers = await usersTable.Take(100).ToCollectionAsync();
            ListUsers.ItemsSource = onlineUsers;
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
                if (isEnabled)
                {
                    Login.Visibility = Visibility.Collapsed;
                    Lgout.Visibility = Visibility.Visible;
                    ListItems.Visibility = Visibility.Visible;
                    ListUsers.Visibility = Visibility.Visible;
                }
                else if (!isEnabled)
                {
                    Login.Visibility = Visibility.Visible;
                    Lgout.Visibility = Visibility.Collapsed;
                    ListItems.Visibility = Visibility.Collapsed;
                    ListUsers.Visibility = Visibility.Collapsed;
                }
                if (dispatcherTimer.IsEnabled)
                {
                    dispatcherTimer.Stop();
                }
                TextInput.PlaceholderText = (isEnabled) ? "type your message here.." : "please login to chat";
                TextInput.IsEnabled = isEnabled;
                btnWinSend.IsEnabled = isEnabled;
                ListItems.Focus(FocusState.Programmatic);
                RefreshChatItems();
                
                TextUserName.Text = "Goodbye, " + userUniqueID;
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
                    // await UpdateStatus("Chat channel is ready.", false);

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
                    int n = 50;
                    // refreshes the entries in the list view by querying the ChatItems table.
                    items = await chatTable.OrderByDescending(chatitem => chatitem.TimeStamp).Take(n).ToCollectionAsync();
                    // reverse the order again so the last item is always at the bottom of the list, not the top
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
                    lastCount = items.Count;
                    ScrollDown();
                }
            }
            prgBusy.IsActive = false;
        }

        // Forces the chat window to scroll to the bottom. 
        private void ScrollDown()
        {
            //ListItems.SelectedIndex = ListItems.Items.Count - 1;
            ListItems.ScrollIntoView(ListItems.Items.LastOrDefault());

        }

        // Event handler for the Send App Bar Button
        private void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            SendChatLine();
        }

        // Prepares the chat item to be sent to the cloud
        private void SendChatLine()
        {
            string msg = TextInput.Text.Trim();
            if (isLoggedin && msg.Length > 0)
            {
                DateTimeOffset myDTO = (DateTimeOffset) GetCurrentTime();
                DateTime utc = myDTO.UtcDateTime;
                var chatItem = new ChatItem { Text = msg, UserName = String.Format("{0}", userUniqueID), TimeStamp = utc };
                //lastChatline = chatItem.Text;
                InsertChatItem(chatItem);
                TextInput.Text = "";
                //RefreshChatItems();
            }
        }

        public static DateTimeOffset? GetCurrentTime()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var result = client.GetAsync("https://google.com",
                            HttpCompletionOption.ResponseHeadersRead).Result;
                    return result.Headers.Date;
                }
                catch
                {
                    return null;
                }
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

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void TextInput_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendChatLine();
            }
        }
    }
}