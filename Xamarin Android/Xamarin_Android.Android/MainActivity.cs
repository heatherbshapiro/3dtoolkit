using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

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
            text.SetText("I love my job", TextView.BufferType.Editable);
			
			button.Click += delegate {
                Intent intent = new Intent(this, typeof(RenderSceneActivity));
                StartActivity(intent);


            };
		}
	}
}


