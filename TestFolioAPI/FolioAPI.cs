using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using System.Xml;
using System.Security.Cryptography;
using Newtonsoft.Json;


namespace TestFolioAPI
{


    [DataContractAttribute]
    class Account
    {
        [DataMemberAttribute]
        public string accountName { get; set; }
        [DataMemberAttribute]
        public string accountType { get; set; }
        [DataMemberAttribute]
        public string billingPlanOid { get; set; }
        [DataMemberAttribute]
        public string primaryAccountOwner { get; set; }
     };

    class FolioApiHandler
    {
        private string _apiKey;
        private string _userKey;
        private DateTime _akTimeStamp;
        private string _pw;
        private string _em;

        public XmlDocument callFolioApi(/*string call, FormUrlEncodedContent content*/)
        {
            string messageBody = string.Empty;

            var v = new {
                loginId = "tester0003",
                membershipType = "N",
                firstName = "John",
                lastName = "Doe",
                dateOfBirth = "1970-05-10",
                tid = "100020001",
                email1 = "william@howardcm.com",
                primaryAddress = new {
                    line1 = "101 Howard Ln",
                    city = "Atlanta",
                    state = "GA",
                    zipcode = "33625",
                   country = "US"
                },
                eveningTelephone = "5512253365",
                dayTelephone = "5542252526",
                employmentStatus = "Retired",
                finraAffiliated = "false",
                directorOrTenPercentShareholder = "false",
                citizenship = "C",
                residenceCountry = "US",
            };


            messageBody = JsonConvert.SerializeObject(v);

            // this works with empty message body
            messageBody = string.Empty;
            string foliourl = "https://testapi.foliofn.com/restapi/accounts";
            string httpMethod = "GET";

            //string foliourl = "https://testapi.foliofn.com/restapi/members";
            //string httpMethod = "POST";

            string authheader = CreateFolioFnSignature(foliourl, httpMethod, messageBody);

            string result = CallFolioApi(foliourl, httpMethod, messageBody, authheader);

            XmlDocument x = CallFolioApi2(foliourl, httpMethod, messageBody, authheader);
            return x;
        }

        // alternate method using HttpClient
        private static XmlDocument CallFolioApi2(string foliourl, string httpMethod, string messageBody, string authheader)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("FOLIOWS", authheader);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.BaseAddress = new Uri(foliourl);

                HttpResponseMessage result;
                if (httpMethod == "GET")
                {
                    result = client.GetAsync(client.BaseAddress).Result;
                }
                else //***** assuming it must be a POST
                {
                    result = /*await*/ client.PostAsync(client.BaseAddress, new StringContent(messageBody, Encoding.UTF8, "application/json")).Result;
                }

                string resultContent = result.Content.ReadAsStringAsync().Result;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(resultContent);
                return xmlDoc;
            }
        }

        private static string CallFolioApi(string foliourl, string httpMethod, string messageBody, string authheader)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(foliourl);
            request.Method = httpMethod;
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "FOLIOWS " + authheader;

            if (messageBody != String.Empty)
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(messageBody);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }

            string result;
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }
            return result;
        }

        private string CreateFolioFnSignature(string foliourl, string httpMethod, string messageBody)
        {
            const string loginid = "folioapi2";
            const string sharedsecret = "gVrrxLfawHaGnRi7lIsjO9Enx79ZpT1yadDQzycb";
            const string apikey = "p63rpmdPgdNIM92WwEkj";

            // get the timestamp 
            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

            // create md5 hash of message body
            string md5Hash = CreateMD5(messageBody);

            // build request signature
            string requeststring = httpMethod.ToUpper() + "\n" + foliourl + "\n" + timestamp + "\n" + md5Hash;

            // Encode requeststring in UTF-8 
            UTF8Encoding utf8 = new UTF8Encoding();
            Byte[] utf8Requeststring = utf8.GetBytes(requeststring);

            // encode shared secret in UTF-8
            Byte[] utf8Sharedsecret = utf8.GetBytes(sharedsecret);

            // get SHA256 hash
            byte[] hmacSha256Hash; // = HashHMAC(utf8_sharedsecret, utf8_requeststring);
            using (HMACSHA256 hmac = new HMACSHA256(utf8Sharedsecret))
            {
                hmacSha256Hash = hmac.ComputeHash(utf8Requeststring);
            }

            // encode string into base64
            string base64Request = Convert.ToBase64String(hmacSha256Hash);

            // URL encode the result, make sure to use upper case in the hex numbers, as Folio requires that
            string signature = UpperCaseUrlEncode(base64Request);

            // construct the auth header
            string authheader = "FOLIOWS_API_KEY=" + apikey + ", FOLIOWS_MEMBER_ID=" + loginid + ", FOLIOWS_SIGNATURE=" + signature + ", FOLIOWS_TIMESTAMP=" + UpperCaseUrlEncode(timestamp);
            return authheader;
        }

        public string UpperCaseUrlEncode(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            char[] temp = HttpUtility.UrlEncode(s).ToCharArray();
            for (int i = 0; i < temp.Length - 2; i++)
            {
                if (temp[i] != '%') 
                    continue;
                temp[i + 1] = char.ToUpper(temp[i + 1]);
                temp[i + 2] = char.ToUpper(temp[i + 2]);
            }
            return new string(temp);
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public void Initialize(string email, string password, string userKey)
        {
            _userKey = userKey;
            _pw = password;
            _em = email;

            FormUrlEncodedContent content = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("email", email),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("user_key", userKey)
            }
            );

            XmlDocument xmlDoc = callFolioApi(/*"https://pi.pardot.com/api/login/version/3", content*/);

            XmlNodeList nodes = xmlDoc.SelectNodes("//rsp/api_key");
            _apiKey = nodes[0].InnerText;
            _akTimeStamp = DateTime.Now;
        }

        public string GetApiKey()
        {
            TimeSpan diff = DateTime.Now - _akTimeStamp;
            TimeSpan oneHour = new TimeSpan(0, 1, 0, 0);
            if (diff >= oneHour)                             // update the api_key if it's over an hour old
                Initialize(_em, _pw, _userKey);
            return _apiKey;
        }

        public XmlDocument GetProspectDetails(string name, int start)
        {
            string listid = GetMailingListWithName(name);

            var keyValues = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("user_key", _userKey), 
            new KeyValuePair<string, string>("api_key", GetApiKey()), 
            new KeyValuePair<string, string>("list_id", listid)
        };
            if (start != 0)
            {
                keyValues.Add(new KeyValuePair<string, string>("offset", start.ToString()));
            }
            var content = new FormUrlEncodedContent(keyValues);

            XmlDocument xmlDoc = callFolioApi(/*"https://pi.pardot.com/api/prospect/version/3/do/query?", content*/);
            return xmlDoc;
        }

        //public rspResultProspect[] GetProspectDetailsArray(string name, int start = 0)
        //{
        //    XmlDocument xmlDoc = GetProspectDetails(name, start);

        //    XmlSerializer serializer = new XmlSerializer(typeof(rsp));
        //    StringReader reader = new StringReader(xmlDoc.InnerXml);
        //    rspResultProspect[] subscriberList = ((rsp)serializer.Deserialize(reader)).result.prospect;

        //    return subscriberList;
        //}

        public XmlDocument CreateProspect(string email, string firstname, string lastname, string picture)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("user_key", _userKey),
                new KeyValuePair<string, string>("api_key", GetApiKey()),
                new KeyValuePair<string, string>("first_name", firstname),
                new KeyValuePair<string, string>("last_name", lastname),
                new KeyValuePair<string, string>("Optimizer_Partner_picture", picture)
            });

            XmlDocument xmlDoc = callFolioApi(/*"https://pi.pardot.com/api/prospect/version/3/do/create/email/" + email, content*/);
            return xmlDoc;
        }

        public XmlDocument UpdateProspectPicture(string email, string picture)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("user_key", _userKey),
                new KeyValuePair<string, string>("api_key", GetApiKey()),
                new KeyValuePair<string, string>("Optimizer_info_block_HTML", picture)
            });

            XmlDocument xmlDoc = callFolioApi(/*"https://pi.pardot.com/api/prospect/version/3/do/update/email/" + email, content*/);
            return xmlDoc;
        }

        public XmlDocument AddProspectToEmailList(string email, string listid)
        {   
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("list_" + listid, "1"),
                new KeyValuePair<string, string>("user_key", _userKey),
                new KeyValuePair<string, string>("api_key", GetApiKey())
            });

            XmlDocument xmlDoc = callFolioApi(/*"https://pi.pardot.com/api/prospect/version/3/do/update/email/" + email, content*/);
            return xmlDoc;
        }

        public XmlDocument RemoveProspectFromEmailList(string email, string listid)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("list_" + listid, "0"),
                new KeyValuePair<string, string>("user_key", _userKey),
                new KeyValuePair<string, string>("api_key", GetApiKey())
            });

            XmlDocument xmlDoc = callFolioApi(/*"https://pi.pardot.com/api/prospect/version/3/do/update/email/" + email, content*/);
            return xmlDoc;
        }

        public string GetMailingListWithName(string name)
        {
            XmlDocument xmlDoc = GetMailingLists();

            XmlNodeList nodes = xmlDoc.SelectNodes("//rsp/result/list[name[text()='" + name + "']]/id");
            if (nodes.Count == 1)
                return nodes[0].InnerText;
            return string.Empty;
        }

        public XmlDocument GetMailingLists()
        {
            var content = new FormUrlEncodedContent(new[] 
                {
                    new KeyValuePair<string, string>("user_key", _userKey),
                    new KeyValuePair<string, string>("api_key", GetApiKey())
                }
            );

            XmlDocument xmlDoc = callFolioApi(/*"https://pi.pardot.com/api/list/version/3/do/query", content*/);
            return xmlDoc;
        }
    }
}
