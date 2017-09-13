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
using Org.Json;
using Newtonsoft.Json.Linq;
using Java.Lang;

namespace Xamarin_Android.Droid
{
    [Activity(Label = "VideoStream")]
    public class VideoStream : Activity, PeerConnection.IObserver, ISdpObserver, IVideoCapturer
    {
        public ISdpObserver sdp;

        public PeerConnection.IObserver observer;
       
        public IVideoCapturer videoCapturer;

        public bool IsScreencast
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            PeerConnectionFactory.InitializeAndroidGlobals(this, true, false, false);
            string vstream = Intent.GetStringExtra("video_stream");

            PeerConnection.IceServer ice = new PeerConnection.IceServer("turnserver3dstreaming.centralus.cloudapp.azure.com:5349", "user", "3Dtoolkit072017");
            IList<PeerConnection.IceServer> servers = new List<PeerConnection.IceServer>();

            servers.Add(ice);

            PeerConnectionFactory pcFactory = new PeerConnectionFactory();
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.VideoStream);
            var videoView = FindViewById<VideoView>(Resource.Id.SampleVideoView);

            // First we create an AudioSource then we can create our AudioTrack
            MediaConstraints audioConstraints = new MediaConstraints();
            MediaConstraints sdpConstraints = new MediaConstraints();
            sdpConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveAudio", "true"));
            sdpConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveVideo", "true"));
            AudioSource audioSource = pcFactory.CreateAudioSource(audioConstraints);
            AudioTrack localAudioTrack = pcFactory.CreateAudioTrack("sad", audioSource);
            
            VideoSource videoSource = pcFactory.CreateVideoSource(videoCapturer);
            VideoTrack localVideoTrack = pcFactory.CreateVideoTrack("vidtrack", videoSource);
            
            // We start out with an empty MediaStream object, created with help from our PeerConnectionFactory
            //  Note that LOCAL_MEDIA_STREAM_ID can be any string
            MediaStream mediaStream = pcFactory.CreateLocalMediaStream("heather");
            mediaStream.AddTrack(localAudioTrack);
            mediaStream.AddTrack(localVideoTrack);
            
            PeerConnection peerConnection = pcFactory.CreatePeerConnection(servers, sdpConstraints,observer);
            peerConnection.InvokeSignalingState();
            peerConnection.AddStream(mediaStream);
            peerConnection.CreateOffer(sdp, sdpConstraints);
            peerConnection.CreateAnswer(sdp, audioConstraints);
            //var videoView = FindViewById<VideoView>(Resource.Id.SampleVideoView);
            //// var uri = Android.Net.Uri.Parse("http://ia600507.us.archive.org/25/items/Cartoontheater1930sAnd1950s1/PigsInAPolka1943.mp4");

            videoView.SetVideoURI(Android.Net.Uri.Parse(ice.Uri));
            videoView.Visibility = ViewStates.Visible;
            videoView.Start();
        }

        public void OnAddStream(MediaStream p0)
        {
            throw new NotImplementedException();
        }

        public void OnAddTrack(RtpReceiver p0, MediaStream[] p1)
        {
            throw new NotImplementedException();
        }

        public void OnDataChannel(DataChannel p0)
        {
            throw new NotImplementedException();
        }

        public void OnIceCandidate(IceCandidate p0)
        {
            throw new NotImplementedException();
        }

        public void OnIceCandidatesRemoved(IceCandidate[] p0)
        {
            throw new NotImplementedException();
        }

        public void OnIceConnectionChange(PeerConnection.IceConnectionState p0)
        {
            throw new NotImplementedException();
        }

        public void OnIceConnectionReceivingChange(bool p0)
        {
            throw new NotImplementedException();
        }

        public void OnIceGatheringChange(PeerConnection.IceGatheringState p0)
        {
            throw new NotImplementedException();
        }

        public void OnRemoveStream(MediaStream p0)
        {
            throw new NotImplementedException();
        }

        public void OnRenegotiationNeeded()
        {
            throw new NotImplementedException();
        }

        public void OnSignalingChange(PeerConnection.SignalingState p0)
        {
            throw new NotImplementedException();
        }

        public void OnCreateFailure(string p0)
        {
            throw new NotImplementedException();
        }

        public void OnCreateSuccess(SessionDescription p0)
        {
            throw new NotImplementedException();
        }

        public void OnSetFailure(string p0)
        {
            throw new NotImplementedException();
        }

        public void OnSetSuccess()
        {
            throw new NotImplementedException();
        }

        public void ChangeCaptureFormat(int p0, int p1, int p2)
        {
            throw new NotImplementedException();
        }

        public void Initialize(SurfaceTextureHelper p0, Context p1, IVideoCapturerCapturerObserver p2)
        {
            throw new NotImplementedException();
        }

        public void StartCapture(int p0, int p1, int p2)
        {
            throw new NotImplementedException();
        }

        public void StopCapture()
        {
            throw new NotImplementedException();
        }
       
        public object getVideoCapturer()
        {
            string[] cameraFacing = { "front", "back" };
            int[] cameraIndex = { 0, 1 };
            int[] cameraOrientation = { 0, 90, 180, 270 };
            foreach (string facing in cameraFacing)
            {
                foreach (int index in cameraIndex)
                {
                    foreach (int orientation in cameraOrientation)
                    {
                        string name = "Camera " + index + ", Facing " + facing +
                            ", Orientation " + orientation;
                        IVideoCapturer capturer = create(name);
                        if (capturer != null)
                        {
                            Console.Write("Using camera: " + name);
                            return capturer;
                        }
                    }
                }
            }
            throw new RuntimeException("Failed to open capturer");
        }
        public IVideoCapturer create(string device_name)
        {
            return null;
        }
    }
}