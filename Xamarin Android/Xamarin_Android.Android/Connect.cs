using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using System.IO;

namespace Xamarin_Android.Droid
{
    class Connect
    {
        public const string url = "http://3dtoolkit-signaling-server.azurewebsites.net";
        public const string port = ":80";
        public void GetServerList()
        {
            //Creates an HTTP request using the required URL
            var req = new HttpWebRequest(new Uri (url + "/sign_in?peer_name=" + port));
            req.ContentType = "text/html";
            req.Method = "GET";

            using (HttpWebResponse response = req.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine("Cannot connect to server");
                }
                using (StreamReader reader= new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        Console.Out.WriteLine("Response contained empty body...");

                    }
                    else
                    {
                        Console.WriteLine("Response Body: \r\n {0}", content);
                    }


                }
            }

            }




        }
    }
