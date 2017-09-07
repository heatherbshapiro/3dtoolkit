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

            ListAdapter = new ArrayAdapter<string>(this, Resource.Layout.ServerList, servers);

            ListView.TextFilterEnabled = true;

            ListView.ItemClick += delegate (object sender, AdapterView.ItemClickEventArgs args) {
                // When clicked, show a toast with the TextView text
                //Toast.MakeText(Application, ((TextView)args.View).Text, ToastLength.Short).Show();
                // Here is where we move to video screening
                Intent intent = new Intent(this, typeof(VideoStream));
                StartActivity(intent);
            };
        }

        //static readonly string[] servers = new String[] {
        //    "server1", "server2", "server3", "server4"
        // };

    }
}