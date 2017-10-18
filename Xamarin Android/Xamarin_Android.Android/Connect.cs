﻿using System;
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
        public const string url = "https://3dtoolkit-signaling-server.azurewebsites.net";
        //public const string url = "http://127.0.0.1:3000";
        //public const string localName = "local";
        public string GetServerList(string clientName)
        {
            //Creates an HTTP request using the required URL
            var req = new HttpWebRequest(new Uri (url + "/sign_in?peer_name=" + clientName));
            req.ContentType = "application/json";
            req.Method = "GET";
            req.Headers["Peer-Type"] = "Client";

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
                        return null;
                    }
                    else
                    {
                        Console.WriteLine("Response Body: \r\n {0}", content);
                        return content;
                    }


                }
            }

            }




        }
    }
