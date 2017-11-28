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
        public const string AzureMobileServicesURI = "https://g00309646.azurewebsites.net";

        // Azure Notification Hub secrets
        public const string AzureNotificationHubName = "g00309646";
        public const string AzureNotificationHubCnxString = "Endpoint=sb://g00309646.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=RlAd8FeJNRR8pjrLsuYaK28el919Z3Zm9be1FZXKzv4=";

        // Azure Mobile Services secrets
        public const string FacebookAppID = "517130681996083";
        public const string WindowsStoreID = "S-1-15-2-3671549834-2253290178-112162265-1390719193-1420205262-4050387950-1987153425";


        // Details for support
        public const string DeveloperSupportEmail = "g00309646@gmit.ie";
    }
}
