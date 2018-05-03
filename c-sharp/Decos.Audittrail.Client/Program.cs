using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Decos.Audittrail.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            // Log 1000 random audit actions
            string[] userNames = { "Peter", "John", "Mike" };
            string[] actions = { "Create", "Delete", "Update", "Read" };

            Random r = new Random(DateTime.Now.Millisecond); // This could probably use a better seed

            for (int i = 0; i < 1000; i++)
            {
                // These values get individually hashed into the blockchain storage. That way, we can reproduce the data from a known
                // list of users, actions and items - but at the same time we don't disclose the actual data in the blockchain, which is
                // stored on Decos' servers.
                string auditActor = userNames[r.Next(userNames.Length - 1)];
                string auditIntent = actions[r.Next(actions.Length - 1)];
                string auditObject = Guid.NewGuid().ToString();

                // Details can be arbitrary data, like before/after, what value changed, related data that was affected, whatever you like. 
                // This data cannot be reproduced in case this data gets lost in your own storage. The blockchain only stores a hash of this entire
                // data block for verification purposes.
                Dictionary<string, string> details = new Dictionary<string, string>();
                details.Add("detail1", Guid.NewGuid().ToString());
                details.Add("detail2", Guid.NewGuid().ToString());
                details.Add("detail3", Guid.NewGuid().ToString());

                string[] hashes = LogAudit(auditActor, auditIntent, auditObject, details);
            }

        }

        // This does the actual audit logging, and returns a string array of 2 elements, the first is the RecordHash, and the second is the TransactionHash. Both 
        // need to be preserved in the audit database we're trying to preserve/secure
        static string[] LogAudit(string auditActor, string auditIntent, string auditObject, Dictionary<string, string> details)
        {
            // Build the audit record JSON structure. this looks like this:
            // {
            //    "header" :  {
            //        "actor" : "name of user doing something",
            //        "intent" : "what is the actor doing",
            //        "object" : "what is the actor doing that on/with"
            //    }
            //    "details" : [
            //        { 
            //            "k" : "key of detail 1",
            //            "v" : "value of detail 1"
            //        },
            //        { 
            //            "k" : "key of detail 2",
            //            "v" : "value of detail 2"
            //        },
            //        ...(more details)...
            //    ]
            // }

            JObject jsonHeader = new JObject(
                new JProperty("actor", auditActor),
                new JProperty("intent", auditIntent),
                new JProperty("object", auditObject)
             );

            JArray jsonDetails = new JArray();
            foreach(var detail in details)
            {
                jsonDetails.Add(
                    new JObject(
                        new JProperty("k", detail.Key),
                        new JProperty("v",detail.Value)
                    )
                );
            }

            JObject jsonPayload = new JObject(
                new JProperty("header", jsonHeader),
                new JProperty("details", jsonDetails)
            );

            // Then post this to the proper endpoint (default: http://localhost:8585/audit , port is configurable in the audit client)
            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
            string jsonReply = wc.UploadString("http://localhost:8585/audit", jsonPayload.ToString());

            JObject reply = JObject.Parse(jsonReply);

            return new string[] { reply["recordHash"].Value<string>(), reply["transactionHash"].Value<string>() };
        }
    }
}
