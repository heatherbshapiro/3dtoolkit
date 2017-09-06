using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Webkit;

namespace Xamarin_Android.Droid
{
    [Activity(Label = "RenderSceneActivity")]
    public class RenderSceneActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.SceneLayout);

            // Create your application here
            WebView localWebView = FindViewById<WebView>(Resource.Id.LocalWebView);
            localWebView.SetWebViewClient(new WebViewClient()); // stops request going to Web Browser
            localWebView.LoadUrl("http://developer.xamarin.com");

        }
    }
}