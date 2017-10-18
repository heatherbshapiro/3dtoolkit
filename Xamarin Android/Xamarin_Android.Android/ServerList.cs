using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Xamarin_Android.Droid
{
    [Activity(Label = "ServerList")]
    public class ServerList : ListActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);
            string ServerList = Intent.GetStringExtra("server_list");
            string[] server = ServerList.Split();
            List<string> servers = server.ToList();
            servers.RemoveAt(0);

            ListAdapter = new ArrayAdapter<string>(this, Resource.Layout.ServerList, servers);

            ListView.TextFilterEnabled = true;

            ListView.ItemClick += delegate (object sender, AdapterView.ItemClickEventArgs args) {
                // When clicked, show a toast with the TextView text
                // Toast.MakeText(Application, ((TextView)args.View).Text, ToastLength.Short).Show();
                // Here is where we move to video screening

                string clicked = servers[args.Position].ToString();

                string peerId = servers[args.Position].ToString().Split(',')[1];
                Console.WriteLine("peerid + " + peerId);
                Intent intent = new Intent(this, typeof(VideoStream));
                intent.PutExtra("sender", peerId);
                StartActivity(intent);
            };
        }

    }
}