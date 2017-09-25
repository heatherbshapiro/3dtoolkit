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
using Android.Graphics;
using Android.Hardware.Display;
using Android.Media.Projection;
using static Org.Webrtc.DataChannel;

namespace Xamarin_Android.Droid
{
    [Activity(Label = "VideoStream")]
    public class VideoStream : Activity, PeerConnection.IObserver, ISdpObserver, IVideoCapturer
    {
        // Observes SDP-related events
        //public PeerConnection peerConnection { get; set; }

        //public PeerConnection.IObserver observer { get; set; }

        //public IVideoCapturer videoCapturer { get; set; }

        private VideoRenderer.ICallbacks remoteRender;
        private VideoRenderer.ICallbacks localRender;
        Org.Webrtc.SurfaceViewRenderer remoteRenderView;
        Org.Webrtc.SurfaceViewRenderer localRenderView;
        private VideoTrack remoteVideoTrack;
        //private EglBase rootEglBase;


        //HEATHER: public ISdpObserver sdp { get; set; }
        
        private IVideoCapturerCapturerObserver capturerObserver { get; set; }
        private SurfaceTextureHelper surfaceTextureHelper { get; set; }
       
        private int width;
        private int height;
        private MediaProjection mediaProjection;
        private bool isDisposed = false;
        private MediaProjectionManager mediaProjectionManager;
        private Intent mediaProjectionPermissionResultData;
        private MediaProjection.Callback mediaProjectionCallback;
        private SurfaceViewRenderer localView;

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
            PeerConnection.IObserver observer = new VideoStream();
            ISdpObserver sdpObserver = new VideoStream();
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.VideoStream);
            //remoteRenderView = FindViewById<Org.Webrtc.SurfaceViewRenderer>(Resource.Id.remote_video_view);
            localRenderView = FindViewById<Org.Webrtc.SurfaceViewRenderer>(Resource.Id.local_video_view);
            localRenderView.Init(EglBase.Create().EglBaseContext, null);

            //localRenderView.SetZOrderMediaOverlay(true);
            //localRenderView.SetEnableHardwareScaler(true);
            //remoteRenderView.SetEnableHardwareScaler(true);
            localRenderView.SetMinimumHeight(680);
            localRenderView.SetMinimumWidth(1024);
            updateVideoView();

            //string vstream = Intent.GetStringExtra("video_stream");

            PeerConnection.IceServer ice = new PeerConnection.IceServer("turnserver3dstreaming.centralus.cloudapp.azure.com:5349", "user", "3Dtoolkit072017");

            //PeerConnection.IceTransportsType type = PeerConnection.IceTransportsType.Relay;

            List<PeerConnection.IceServer> servers = new List<PeerConnection.IceServer>();
            servers.Add(ice);

            /* Handles the creating of audio and video streams. */
            PeerConnectionFactory.InitializeAndroidGlobals(this, true, true, true);


            PeerConnectionFactory pcFactory = new PeerConnectionFactory();


            /* Set initial audio and video constraints */
            MediaConstraints audioConstraints = new MediaConstraints();
            MediaConstraints sdpConstraints = new MediaConstraints();
            sdpConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveAudio", "true"));
            sdpConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveVideo", "true"));

            /* Create the local VideoCapturer (for now) and tracks, of front facing camera and audio.*/

            IVideoCapturer videoCapturer = createVideoCapturer();
            VideoSource videoSource = pcFactory.CreateVideoSource(videoCapturer);
            //videoCapturer.StartCapture(100, 100, 30); //change these constants
            VideoTrack localVideoTrack = pcFactory.CreateVideoTrack("vidtrack", videoSource);
            //localVideoTrack.AddRenderer(new VideoRenderer(localRender));

            AudioSource audioSource = pcFactory.CreateAudioSource(audioConstraints);
            AudioTrack localAudioTrack = pcFactory.CreateAudioTrack("sad", audioSource);
            
            /* Add local stracks to the Media Stream. This is how local audio/video displays on your phone*/
            MediaStream mediaStream = pcFactory.CreateLocalMediaStream("heather");
            mediaStream.AddTrack(localAudioTrack);
            mediaStream.AddTrack(localVideoTrack);
                                           
            PeerConnection peerConnection = pcFactory.CreatePeerConnection(servers, sdpConstraints, observer);
            peerConnection.AddStream(mediaStream);
                        
            peerConnection.CreateOffer(sdpObserver, sdpConstraints);
            peerConnection.CreateAnswer(sdpObserver, audioConstraints);

            // var uri = Android.Net.Uri.Parse("http://ia600507.us.archive.org/25/items/Cartoontheater1930sAnd1950s1/PigsInAPolka1943.mp4");

            // HEATHER surfaceTextureHelper = SurfaceTextureHelper.Create("current", new EglBaseContext());            
            // HEATHER videoCapturer.Initialize(surfaceTextureHelper,this, capturerObserver);
            
            //videoView.SetVideoURI(Android.Net.Uri.Parse(ice.Uri));
            //videoView.Visibility = ViewStates.Visible;
            //videoView.Start();
        }

        public void OnAddStream(MediaStream mediaStream)
        {
            Console.WriteLine("ADDED STREAM");
            if (mediaStream.VideoTracks.Size() == 0)
            {
                Console.WriteLine("onAddStream", "NO REMOTE STREAM");

            }
            //mediaStream.videoTracks.get(0).addRenderer(new VideoRenderer(remoteRender));

        }

        public void OnAddTrack(RtpReceiver p0, MediaStream[] p1)
        {
            throw new NotImplementedException();
        }

        public void OnDataChannel(DataChannel p0)
        {
            throw new NotImplementedException();
        }

        public void OnIceCandidate(IceCandidate candidate)
        {
            Console.WriteLine("FOUND AN ICE CANDIDATE");
            if(candidate != null)
            {
                JSONObject payload = new JSONObject();
                payload.Put("sdpMLineIndex", candidate.SdpMLineIndex);
                payload.Put("sdpMid", candidate.SdpMid);
                payload.Put("candidate", candidate.Sdp);
                JSONObject candidateObject = new JSONObject();
                candidateObject.Put("ice", candidate.ToString());
                sendText(candidateObject.ToString());
            }
        }

        private void sendText(string v)
        {
            throw new NotImplementedException();
        }

        public void OnIceCandidatesRemoved(IceCandidate[] p0)
        {
            Console.WriteLine("REMOVED ICE CANDIDATE");
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
            Console.WriteLine("Ice GATHERING CHANGED");
        }

        public void OnRemoveStream(MediaStream p0)
        {
            throw new NotImplementedException();
        }

        public void OnRenegotiationNeeded()
        {
            throw new NotImplementedException();
        }

        public void OnSignalingChange(PeerConnection.SignalingState signalingState)
        {
            Console.WriteLine("SIGNAL CHANGED");
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

        //private IVideoCapturer getVideoCapturer()
        private IVideoCapturer createVideoCapturer()
        {
            IVideoCapturer capturer;

            if (useCamera2())
            {
                capturer = createCameraCapturer(new Camera2Enumerator(this));
            }
            else
            {
                capturer = createCameraCapturer(new Camera1Enumerator(true));
            }
            if (capturer == null)
            {
                return null;
            }
            return capturer;
        }

        private IVideoCapturer createCameraCapturer(ICameraEnumerator enumerator)
        {
            string[] deviceNames = enumerator.GetDeviceNames();

            // First, try to find front facing camera
            foreach (string deviceName in deviceNames)
            {
                if (enumerator.IsFrontFacing(deviceName))
                {
                    //Logging.d(LOG_TAG, "Creating front facing camera capturer.");
                    IVideoCapturer capturer = enumerator.CreateCapturer(deviceName, null);

                    if (capturer != null)
                    {
                        return capturer;
                    }
                }
            }

            // Front facing camera not found, try something else
            foreach (string deviceName in deviceNames)
            {
                if (!enumerator.IsFrontFacing(deviceName))
                {
                    IVideoCapturer capturer = enumerator.CreateCapturer(deviceName, null);
                    
                    if (capturer != null)
                    {
                        return capturer;
                    }
                }
            }

            return null;
        }

        private bool useCamera2()
        {
            return Camera2Enumerator.IsSupported(this);
        }

        private void updateVideoView()
        {
            //FrameLayout remoteVideoLayout = FindViewById<FrameLayout>(Resource.Id.remote_video_layout);
            FrameLayout localVideoLayout = FindViewById<FrameLayout>(Resource.Id.local_video_layout);
            //remoteVideoLayout.setPosition(REMOTE_X, REMOTE_Y, REMOTE_WIDTH, REMOTE_HEIGHT);
            //remoteRenderView.SetScalingType(SCALE_ASPECT_FILL);
            //remoteRenderView.SetMirror(false);

            /*if (iceConnected)
            {
                //localVideoLayout.setPosition(
                //        LOCAL_X_CONNECTED, LOCAL_Y_CONNECTED, LOCAL_WIDTH_CONNECTED, LOCAL_HEIGHT_CONNECTED);
                localRenderView.SetScalingType(SCALE_ASPECT_FIT);
            }
            else
            {
                //localVideoLayout.setPosition(
                //        LOCAL_X_CONNECTING, LOCAL_Y_CONNECTING, LOCAL_WIDTH_CONNECTING, LOCAL_HEIGHT_CONNECTING);
                localRenderView.SetScalingType(SCALE_ASPECT_FILL);
            }*/

            localRenderView.SetMirror(true);
            localRenderView.RequestLayout();
            //remoteRenderView.RequestLayout();
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
        
    }
}