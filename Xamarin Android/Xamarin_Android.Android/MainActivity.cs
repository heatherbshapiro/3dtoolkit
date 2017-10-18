using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System
    .Threading.Tasks;
using System.Collections.Generic;

namespace Xamarin_Android.Droid
{
	[Activity (Label = "Xamarin_Android.Android", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button button = FindViewById<Button> (Resource.Id.myButton);

            EditText text = FindViewById<EditText>(Resource.Id.URLText);
            EditText nameText = FindViewById<EditText>(Resource.Id.NameText);

            button.Click += delegate {
                Connect connect = new Connect();
                string content = connect.GetServerList(nameText.Text);
                int myId = Int32.Parse(content.ToString().Split(',')[1]);

                Intent intent = new Intent(this, typeof(VideoStream));
                intent.PutExtra("serverUrl", text.Text);
                intent.PutExtra("server_list", content);
                intent.PutExtra("myId", myId.ToString());
                StartActivity(intent);
            };
    
		}
    
    }
}


