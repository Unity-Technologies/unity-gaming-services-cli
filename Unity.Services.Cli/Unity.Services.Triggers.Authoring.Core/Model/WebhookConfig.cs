using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Unity.Services.Triggers.Authoring.Core.Model
{
    [DataContract]
    public class WebhookConfig
    {
        [DataMember]
        public string Url { get; set; }
        [DataMember]
        public string Method { get; set; }
        [DataMember]
        public Dictionary<string, string> Headers { get; set; }
        [DataMember]
        public string PayloadTemplate { get; set; }

        public WebhookConfig()
        {
        }

        public WebhookConfig(string url, string method, Dictionary<string, string> headers, string payloadTemplate)
        {
            Url = url;
            Method = method;
            Headers = headers;
            PayloadTemplate = payloadTemplate;
        }
    }
}
