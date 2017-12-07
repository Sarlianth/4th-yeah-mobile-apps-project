using System;
using Newtonsoft.Json;

namespace myChat
{
    class OnlineUsers
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "username")]
        public string username { get; set; }

        // Read-only property not persisted in the cloud
        [JsonIgnore]
        public string OnlineUser
        {
            get { return this.username; }
        }
    }
}
