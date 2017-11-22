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

        // Details for support
        public const string DeveloperSupportEmail = "g00309646@gmit.ie";
    }
}
