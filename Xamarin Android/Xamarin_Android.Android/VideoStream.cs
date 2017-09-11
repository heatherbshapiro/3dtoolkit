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
using Android.Opengl;

namespace Xamarin_Android.Droid
{
    [Activity(Label = "VideoStream")]
    public class VideoStream : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

           
            PeerConnectionFactory pcFactory = new PeerConnectionFactory();
            PeerConnection.IceServer ice = new PeerConnection.IceServer("turnserver3dstreaming.centralus.cloudapp.azure.com:5349", "user", "3Dtoolkit072017");
            List<PeerConnection.IceServer> servers = new List<PeerConnection.IceServer>();

            servers.Add(ice);


            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.VideoStream);
            var videoView = FindViewById<VideoView>(Resource.Id.SampleVideoView);

            // First we create an AudioSource then we can create our AudioTrack
            MediaConstraints audioConstraints = new MediaConstraints();

            AudioSource audioSource = pcFactory.CreateAudioSource(audioConstraints);
            AudioTrack localAudioTrack = pcFactory.CreateAudioTrack("sad", audioSource);


            // We start out with an empty MediaStream object, created with help from our PeerConnectionFactory
            //  Note that LOCAL_MEDIA_STREAM_ID can be any string
            MediaStream mediaStream = pcFactory.CreateLocalMediaStream("heather");
            mediaStream.AddTrack(localAudioTrack);
            
            
            PeerConnection peerConnection = pcFactory.CreatePeerConnection(servers, audioConstraints,null);

            peerConnection.AddStream(mediaStream);
            //var videoView = FindViewById<VideoView>(Resource.Id.SampleVideoView);


            //// var uri = Android.Net.Uri.Parse("http://ia600507.us.archive.org/25/items/Cartoontheater1930sAnd1950s1/PigsInAPolka1943.mp4");

            videoView.SetVideoURI(Android.Net.Uri.Parse(""));
            videoView.Visibility = ViewStates.Visible;
            videoView.Start();
        }
    }
}