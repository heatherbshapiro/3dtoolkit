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
using Org.Webrtc;

namespace Xamarin_Android.Droid
{
    [Activity(Label = "VideoStream")]
    public class VideoStream : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            string videoStream = Intent.GetStringExtra("video_stream");
            var uri = Android.Net.Uri.Parse(videoStream);


            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.VideoStream);

            var videoView = FindViewById<VideoView>(Resource.Id.SampleVideoView);

           //// var uri = Android.Net.Uri.Parse("http://ia600507.us.archive.org/25/items/Cartoontheater1930sAnd1950s1/PigsInAPolka1943.mp4");

            videoView.SetVideoURI(uri);
            videoView.Visibility = ViewStates.Visible;
            videoView.Start();
        }
    }
}