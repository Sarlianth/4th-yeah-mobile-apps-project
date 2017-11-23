using System;
using Newtonsoft.Json;

namespace myChat
{
    class ChatItem
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "username")]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        public DateTime TimeStamp { get; set; }

        // Read-only property not persisted in the cloud
        [JsonIgnore]
        public string ChatLine
        {
            get { return this.UserName + " - " + this.Text; }
        }
    }
}
