using System;
using System.Collections.Generic;
using System.Text;

namespace myChat
{
    static class ConfigSecrets
    {
        // Once configured the required Azure services set to true (debugging)
        public const bool ISAZURECONFIGDONE = true;

        // Azure Mobile Services secrets
        public const string AzureMobileServicesURI = "YOUR MOBILE SERVICES URI";

        // Azure Notification Hub secrets
        public const string AzureNotificationHubName = "AZURE NOTIFICATION HUB NAME";
        public const string AzureNotificationHubCnxString = "XXXXXX-AZURE NOTIFICATION HUB STRING-XXXXXX"
        // Details for support
        public const string DeveloperSupportEmail = "g00309646@gmit.ie";
    }
}
