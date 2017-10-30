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
	[Activity (Label = "3D Toolkit Login", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class MainActivity : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
            RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;

            SetContentView (Resource.Layout.Main);

			Button connectButton = FindViewById<Button> (Resource.Id.myButton);

            EditText urlText = FindViewById<EditText>(Resource.Id.URLText);
            EditText nameText = FindViewById<EditText>(Resource.Id.NameText);

            connectButton.Click += delegate {
                Connect connect = new Connect();
                string content = connect.GetServerList(nameText.Text);
                Intent intent = new Intent(this, typeof(VideoStream));
                intent.PutExtra("serverUrl", urlText.Text);
                intent.PutExtra("serverList", content);
                StartActivity(intent);
            };
    
		}
    
    }
}


