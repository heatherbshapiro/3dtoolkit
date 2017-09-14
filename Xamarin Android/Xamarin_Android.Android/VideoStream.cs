﻿using System;
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
//using Foundation;
//using AVFoundation; 
using Android.Graphics;
using Android.Hardware.Display;


namespace Xamarin_Android.Droid
{
    [Activity(Label = "VideoStream")]
    public class VideoStream : Activity, PeerConnection.IObserver, ISdpObserver, IVideoCapturer
    {

        public ISdpObserver sdp;

        public PeerConnection.IObserver observer;
       
        public IVideoCapturer videoCapturer;

        private VideoRenderer.ICallbacks remoteRender;
        private VideoRenderer.ICallbacks localRender;
        Org.Webrtc.SurfaceViewRenderer remoteRenderView;
        Org.Webrtc.SurfaceViewRenderer localRenderView;
        private VideoTrack remoteVideoTrack;

        private EglBase rootEglBase;

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

            //rootEglBase = EglBase.Create();
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.VideoStream);
            remoteRenderView = FindViewById<Org.Webrtc.SurfaceViewRenderer> (Resource.Id.remote_video_view);
            localRenderView = FindViewById<Org.Webrtc.SurfaceViewRenderer>(Resource.Id.local_video_view);

            localRenderView.SetZOrderMediaOverlay(true);
            localRenderView.SetEnableHardwareScaler(true);
            remoteRenderView.SetEnableHardwareScaler(true);
            updateVideoView();

            PeerConnectionFactory.InitializeAndroidGlobals(this, true, false, false);
            string vstream = Intent.GetStringExtra("video_stream");

            PeerConnection.IceServer ice = new PeerConnection.IceServer("turnserver3dstreaming.centralus.cloudapp.azure.com:5349", "user", "3Dtoolkit072017");
            IList<PeerConnection.IceServer> servers = new List<PeerConnection.IceServer>();

            //SHOULDNT we also have a stun server? 
            servers.Add(ice);

            PeerConnectionFactory pcFactory = new PeerConnectionFactory();

            // First we create an AudioSource then we can create our AudioTrack
            MediaConstraints audioConstraints = new MediaConstraints();
            MediaConstraints sdpConstraints = new MediaConstraints();
            sdpConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveAudio", "true"));
            sdpConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveVideo", "true"));
            AudioSource audioSource = pcFactory.CreateAudioSource(audioConstraints);
            AudioTrack localAudioTrack = pcFactory.CreateAudioTrack("sad", audioSource);


            //CREATE LOCAL VIDEO TRACK

            videoCapturer = createVideoCapturer();
            VideoSource videoSource = pcFactory.CreateVideoSource(videoCapturer);
            //change these constants
            videoCapturer.StartCapture(100, 100, 30);
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

            //// var uri = Android.Net.Uri.Parse("http://ia600507.us.archive.org/25/items/Cartoontheater1930sAnd1950s1/PigsInAPolka1943.mp4");

            
            //localVideoTrack.AddRenderer(new VideoRenderer(localRender));
            //videoView.SetVideoURI(Android.Net.Uri.Parse(ice.Uri));
            //videoView.Visibility = ViewStates.Visible;
            //videoView.Start();
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

        //public IVideoCapturer getVideoCapturer()
        //{
            
            //string[] cameraFacing = { "front", "back" };
            //int[] cameraIndex = { 0, 1 };
            //int[] cameraOrientation = { 0, 90, 180, 270 };
            //foreach (string facing in cameraFacing)
            //{
            //    foreach (int index in cameraIndex)
            //    {
            //        foreach (int orientation in cameraOrientation)
            //        {
            //            string name = "Camera " + index + ", Facing " + facing +
            //                ", Orientation " + orientation;
            //            System.Diagnostics.Debug.Print(name);
            //            IVideoCapturer capturer = Create(name);
                        
            //            if (capturer != null)
            //            {
                            //Console.Write("Using camera: " + name);
                            //return capturer;
            //            }
            //        }
            //    }
            //}
            //throw new RuntimeException("Failed to open capturer");


        //}

        private IVideoCapturer createVideoCapturer()
        {
            IVideoCapturer videoCapturer;
            if (useCamera2())
            {
                videoCapturer = createCameraCapturer(new Camera2Enumerator(this));
            }
            else
            {
                videoCapturer = createCameraCapturer(new Camera1Enumerator(true));
            }
            if (videoCapturer == null)
            {
                return null;
            }
            return videoCapturer;
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
                    ICameraVideoCapturer videoCapturer = enumerator.CreateCapturer(deviceName, null);

                    if (videoCapturer != null)
                    {
                        return videoCapturer;
                    }
                }
            }

            // Front facing camera not found, try something else
            foreach (string deviceName in deviceNames)
            {
                if (!enumerator.IsFrontFacing(deviceName))
                {
                    //Logging.d(LOG_TAG, "Creating other camera capturer.");
                    IVideoCapturer videoCapturer = enumerator.CreateCapturer(deviceName, null);
                    
                    if (videoCapturer != null)
                    {
                        return videoCapturer;
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
            FrameLayout remoteVideoLayout = FindViewById<FrameLayout>(Resource.Id.remote_video_layout);
            FrameLayout localVideoLayout = FindViewById<FrameLayout>(Resource.Id.local_video_layout);

            //remoteVideoLayout.setPosition(REMOTE_X, REMOTE_Y, REMOTE_WIDTH, REMOTE_HEIGHT);
            //remoteRenderView.SetScalingType(SCALE_ASPECT_FILL);
            //remoteRenderView.SetMirror(false);

            //if (iceConnected)
            //{
            //    //localVideoLayout.setPosition(
            //    //        LOCAL_X_CONNECTED, LOCAL_Y_CONNECTED, LOCAL_WIDTH_CONNECTED, LOCAL_HEIGHT_CONNECTED);
            //    localRenderView.SetScalingType(SCALE_ASPECT_FIT);
            //}
            //else
            //{
            //    //localVideoLayout.setPosition(
            //    //        LOCAL_X_CONNECTING, LOCAL_Y_CONNECTING, LOCAL_WIDTH_CONNECTING, LOCAL_HEIGHT_CONNECTING);
            //    localRenderView.SetScalingType(SCALE_ASPECT_FILL);
            //}
            //localRenderView.SetMirror(true);

            //localRenderView.RequestLayout();
            //remoteRenderView.RequestLayout();
        }

        //public IVideoCapturer Create(string device_name)
        //{
        //    //front-facing only for now.
        //    Camera1Enumerator cameraEnumator = new Camera1Enumerator();
        //    IVideoCapturer videoCapture = cameraEnumator.CreateCapturer(device_name, null);
        //    return videoCapture;

        //}
    }
}