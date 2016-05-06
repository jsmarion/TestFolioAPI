using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TestFolioAPI
{
    class Program
    {
        static void Main(string[] args)
        {        
            string username = "folioapi2";
            //string apikey = "p63rpmdPgdNIM92WwEkj";
            //string sharedsecret = "gVrrxLfawHaGnRi7lIsjO9Enx79ZpT1yadDQzycb";
            string url = "https://testapi.foliofn.com/restapi/accounts?user=" + username;

            var fah = new FolioApiHandler();

            var x = fah.callFolioApi(/*url, content*/);
        }
    }
}
