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
using Android.Media.Projection;
using Android.Graphics;

namespace Xamarin_Android.Droid
{
    [Activity(Label = "VideoStream")]
    public class VideoStream : Activity, PeerConnection.IObserver, ISdpObserver, IVideoCapturer
    {
      
        public ISdpObserver sdp { get; set; }

        public PeerConnection.IObserver observer { get; set; }

        public IVideoCapturer videoCapturer { get; set; }
        private IVideoCapturerCapturerObserver capturerObserver { get; set; }
        private SurfaceTextureHelper surfaceTextureHelper { get; set; }
       
        private int width;
        private int height;
        private MediaProjection mediaProjection;
        private bool isDisposed = false;
        private MediaProjectionManager mediaProjectionManager;
        private Intent mediaProjectionPermissionResultData;
        private MediaProjection.Callback mediaProjectionCallback;


        private void checkNotDisposed()
        {
            if (isDisposed)
            {
                throw new RuntimeException("capturer is disposed.");
            }
        }
        public bool IsScreencast
        {
            get
            {
                return true;
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
            surfaceTextureHelper = SurfaceTextureHelper.Create("current", new EglBaseContext());            
            videoCapturer.Initialize(surfaceTextureHelper,this, capturerObserver);

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

        public void Initialize(SurfaceTextureHelper surfaceTextureHelper, Context applicationContext, IVideoCapturerCapturerObserver capturerObserver)
        {
            if (capturerObserver == null)
            {
                throw new RuntimeException("capturerObserver not set.");
            }
            this.capturerObserver = capturerObserver;
            if (surfaceTextureHelper == null)
            {
                throw new RuntimeException("surfaceTextureHelper not set.");
            }
            this.surfaceTextureHelper = surfaceTextureHelper;
            mediaProjectionManager = (MediaProjectionManager)applicationContext.GetSystemService(
                Context.MediaProjectionService);
        }

        public void StartCapture(int width, int height, int ignoredFramerate)
        {

            this.width = width;
            this.height = height;
            mediaProjection = mediaProjectionManager.GetMediaProjection(
                1, mediaProjectionPermissionResultData);
            //// Let MediaProjection callback use the SurfaceTextureHelper thread.
            //mediaProjection.RegisterCallback(mediaProjectionCallback, surfaceTextureHelper.getHandler());
            //createVirtualDisplay();
            //capturerObserver.OnCapturerStarted(true);
            //surfaceTextureHelper.StartListening();
        }
        private void createVirtualDisplay()
        {
            //surfaceTextureHelper.getSurfaceTexture().setDefaultBufferSize(width, height);
            //virtualDisplay = mediaProjection.createVirtualDisplay("WebRTC_ScreenCapture", width, height,
            //    VIRTUAL_DISPLAY_DPI, DISPLAY_FLAGS, new Surface(surfaceTextureHelper.getSurfaceTexture()),
            //    null /* callback */, null /* callback handler */);
        }
        public void StopCapture()
        {
            throw new NotImplementedException();
        }
       
        private IVideoCapturer getVideoCapturer()
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