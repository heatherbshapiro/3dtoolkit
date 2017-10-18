using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Json;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.IO;

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
using static Android.Opengl.GLSurfaceView;
using Newtonsoft.Json;
using Java.Util.Regex;
using Java.Nio;
using Android.Util;
using static Xamarin_Android.Droid.MatrixMath;

namespace Xamarin_Android.Droid
{
    [Activity(Label = "VideoStreamTest")]
    public class VideoStream : Activity
    {
        public const string url = "https://3dtoolkit-signaling-server.azurewebsites.net";

        static PeerConnection peerConnection;
        PeerConnectionFactory pcFactory;
        static string myId;
        List<string> servers;
        Dictionary<string, string> otherPeers = new Dictionary<string, string>();
        ListView serverList;

        static string peerId;

        int heartBeatIntervalInSecs = 5;
        System.Threading.Timer timer;
        int messageCounter;

        static HttpClient client; 

        static VideoTrack remoteVideoTrack;
        static VideoRendererWithControls remoteVideoRenderer;


        MediaConstraints pcConstraints;
        //MediaConstraints videoConstraints;
        //MediaConstraints audioConstraints;
        MediaStream audioMediaStream;
        MediaConstraints sdpMediaConstraints;
        static DataChannel inputChannel;

        PeerConnection.IObserver pcObserver;
        static ISdpObserver sdpObserver;

        ArrayAdapter<string> adapter;

        static bool isInitiator;
        static bool isLocal;
        static bool isRemote;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Stetho.initializeWithDefaults(this);
            Java.Lang.JavaSystem.LoadLibrary("jingle_peerconnection_so");

            SetContentView(Resource.Layout.VideoStream);

            //var eglBase = Java.Lang.Class.ForName("microsoft.a3dtoolkitandroid.util").NewInstance();

            /* TO DO: Initialize Video Tracks/Renderin. Errors with SurfaceViewRenderer */

            /* Grab server list and create view */
            string ServerList = Intent.GetStringExtra("server_list");

            string[] server = ServerList.Split();
            servers = server.ToList();
            myId = servers[0].Split(',')[1];
            Console.Write("MYID     " + myId.ToString());
            servers.RemoveAt(0);

            for (int i=0; i<servers.Count; i++)
            {
                if (servers[i].Length > 0)
                {
                    string[] info = servers[i].Split(',');
                    otherPeers.Add(info[1], info[0]);
                }
            }

            serverList = FindViewById<ListView>(Resource.Id.server_list);
            adapter = new ArrayAdapter<string>(this, Resource.Layout.VideoStream, Resource.Id.textItem, servers);
            serverList.Adapter = adapter;

            BeginProcess();

            serverList.ItemClick += delegate (object sender, AdapterView.ItemClickEventArgs args)
            {
                string clicked = servers[args.Position].ToString();
                string peer = servers[args.Position].Split(',')[1];
                string serverName = servers[args.Position].Split(',')[0];
                Console.WriteLine("peername: " + serverName);

                JoinPeer(peer);

                IEglBase eglBase = EglBaseFactory.Create();
                var layout = new LinearLayout(this);
                //var video_view = new VideoRendererWithCotnrols(this);
                remoteVideoRenderer = new VideoRendererWithControls(this);
                //remoteVideoRenderer = FindViewById(Resource.Id.remote_video_view);
                //EglBaseContext context = eglBase.GetEglBaseContext();
                //remoteVideoRenderer.SetScaleGestureDetector(new ScaleGestureDetector(this, new MyScaleListener(remoteVideoRenderer)));
                remoteVideoRenderer.Init(eglBase.GetEglBaseContext(), null);
                remoteVideoRenderer.SetEventListener(new MotionEventListener());
                layout.AddView(remoteVideoRenderer);
                SetContentView(layout);

                //RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;

                //remoteVideoRenderer = FindViewById<SurfaceViewRenderer>(Resource.Id.video_view);
            };
        }

        protected void BeginProcess()
        {
            StartHangingGet();
            Console.Write("IM DONE THIS TASK");
            StartHeartBeat();
            //UpdatePeerList(); 
        }

        public void Disconnect()
        {
            // Stop the heartbeat
            if (timer != null)
            {
                timer.Dispose();
            }
            if (peerConnection != null)
            {
                peerConnection.Close();
                peerConnection.Dispose();
                peerConnection = null;
            }
            if (pcFactory != null)
            {
                pcFactory.Dispose();
                pcFactory = null;
            }
            // CLOSE OUT RENDERER, EGL BASE WHEN CREATED. 

            PeerConnectionFactory.StopInternalTracingCapture();
            PeerConnectionFactory.ShutdownInternalTracer();

            if (myId != "-1")
            {
                //Tell the server we are signing out
                var req = new HttpWebRequest(new Uri(url + "/sign_out?peer_id=" + myId));
                req.Method = "GET";
                req.Headers["Peer-Type"] = "Client";

                using (HttpWebResponse response = req.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Console.WriteLine("Cannot connect to server");
                    }
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        var content = reader.ReadToEnd();
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            Console.Out.WriteLine("Response contained empty body...");
                        }
                        else
                        {
                            Console.WriteLine("Response: \r\n {0}", content);
                        }
                    }
                }

                myId = "-1";
            }
        }

        public void JoinPeer(string peer)
        {
            CreatePeerConnection(peer);

            inputChannel = peerConnection.CreateDataChannel("inputDataChannel", new DataChannel.Init());
            inputChannel.RegisterObserver(new DcObserver());

            sdpObserver = new SDPObserver();

            isInitiator = true;
            peerConnection.CreateOffer(sdpObserver, sdpMediaConstraints);
        }

        public void CreatePeerConnection(string peer)
        {
            /* Set this peer to the global variable to observers can access it. */

            //RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;

            peerId = peer;

            /* Supply the media constraints */
            pcConstraints = new MediaConstraints();
            pcConstraints.Optional.Add(new MediaConstraints.KeyValuePair("DtlsSrtpKeyAgreement", "true"));
            pcConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveAudio", "false"));
            pcConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveVideo", "true"));

            // ARE AUDIO AND SDPMEDIA CONSTRAINTS NECESSARY? 
            // audioConstraints = new MediaConstraints();

            sdpMediaConstraints = new MediaConstraints();
            sdpMediaConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveAudio", "false"));
            sdpMediaConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveVideo", "true"));

            /* Create the Ice Server */
            PeerConnection.IceServer ice = new PeerConnection.IceServer("turn:turnserver3dstreaming.centralus.cloudapp.azure.com:5349", "user", "3Dtoolkit072017", PeerConnection.TlsCertPolicy.TlsCertPolicyInsecureNoCheck);
            List<PeerConnection.IceServer> iceServers = new List<PeerConnection.IceServer> { ice };
            PeerConnection.RTCConfiguration rtcConfig = new PeerConnection.RTCConfiguration(iceServers);
            rtcConfig.IceTransportsType = PeerConnection.IceTransportsType.Relay;

            /* Create the observer */
            pcObserver = new PeerObserver();

            /* Initialize the PeerConnectionFactory*/
            PeerConnectionFactory.InitializeAndroidGlobals(this, false, true, true);

            pcFactory = new PeerConnectionFactory();

            /* Establish peer connection */

            peerConnection = pcFactory.CreatePeerConnection(rtcConfig, pcConstraints, pcObserver);
            Console.Write("createPeerConnection: PeerConnection = " + peerConnection.ToString());
        }

        static async Task SendToPeer(string peer, Dictionary<string, string> data)
        {
            if (myId == "-1")
            {
                Console.Write("SendToPeer Not Connected");
                return;
            }
            if (peer == myId)
            {
                Console.Write("SendToPeer Can't send a message to yoself !");
                return;
            }

            client = new HttpClient();

            var json = JsonConvert.SerializeObject(data);
            JObject dataToSend = JObject.Parse(json);
            var content = new StringContent(json, Encoding.UTF8, "text/plain");
            content.Headers.Add("Peer-Type", "Client");
            var req = new Uri(url + "/message?peer_id=" + myId + "&to=" + peer);
            HttpResponseMessage response = null;
            response = await client.PostAsync(req, content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Successfully sent message to peer: {0}", response.Content.ToString());
            }
        }

        public void HandlePeerMessage(string sender, JObject data)
        {
            messageCounter += 1;
            string str = "Message from " + otherPeers.GetValueOrDefault(sender) + ":" + data.ToString();

            Console.WriteLine("Received " + data.ToString());

            bool dataType = data.GetValue("type") == null ? false : true;
            if (dataType)
            {
                if (data.GetValue("type").ToString().Equals("offer"))
                {
                    SessionDescription sdp = new SessionDescription(SessionDescription.Type.Offer, data.GetValue("sdp").ToString());
                    CreatePeerConnection(sender);
                    isRemote = true;
                    peerConnection.SetRemoteDescription(sdpObserver, sdp);
                    peerConnection.CreateAnswer(sdpObserver, sdpMediaConstraints);
                }
                else if (data.GetValue("type").ToString().Equals("answer"))
                {
                    SessionDescription sdp = new SessionDescription(SessionDescription.Type.Answer, data.GetValue("sdp").ToString());
                    isRemote = true;
                    peerConnection.SetRemoteDescription(sdpObserver, sdp);
                }
            }
            else
            {
                // we need to add an ice candidate. (not a message)
                string iceSdp = (string)data.SelectToken("candidate");
                string sdpMid = (string)data.SelectToken("sdpMid");
                int sdpMidLineIndex = Int32.Parse((string)data.SelectToken("sdpMLineIndex"));
                IceCandidate newCandidate = new IceCandidate(sdpMid, sdpMidLineIndex, iceSdp);
                peerConnection.AddIceCandidate(newCandidate);
            }
        }

        private void UpdatePeerList()
        {
            try
            {
                foreach (string peer in servers)
                {
                    Console.Write("data set change");
                    adapter.Clear();
                    adapter.AddAll(servers);
                    adapter.NotifyDataSetChanged();
                }
            }
            catch (System.Exception e)
            {
                Console.Write(e);
            }
        }

        public void HandleServerNotification(string server)
        {
            string[] info = server.Trim().Split(',');
            if (Int32.Parse(info[2]) != 0)
            {
                servers.Add(server);
                otherPeers.Add(info[1], info[0]);
            }
            UpdatePeerList();
        }

        private async Task StartHangingGet()
        {
            // Create an HTTP web request using the URL:
            Uri urlString = new Uri(url + "/wait?peer_id=" + myId);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(urlString);
            request.Method = "GET";
            request.Headers["Peer-Type"] = "Client";

            using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
            {
                try
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Console.WriteLine("Cannot connect to server");
                        Disconnect();
                    }
                    else
                    {
                        HangingGetCallback(response);
                    }
                }
                catch (System.Exception e)
                {
                    Console.WriteLine("SERVER ERROR +  " + e);
                }
            }
        }

        public void HangingGetCallback(HttpWebResponse response)
        {
            Console.Write("IN HANGING GET CALLBACK");

            string data;
            JObject jsonObj;
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                data = reader.ReadToEnd();
            }

            string sender = response.GetResponseHeader("Pragma");

            if (sender == myId)
            {
                HandleServerNotification(data);
            }
            else
            {
                jsonObj = JObject.Parse(data);
                HandlePeerMessage(sender, jsonObj);
            }

            if (myId != "-1")
            {
                /* Restart hanging get. TO DO: Restart when timeout*/
                StartHangingGet();
            }

        }

        public void StartHeartBeat()
        {
            timer = new System.Threading.Timer((e) =>
            {

                var req = new HttpWebRequest(new Uri(url + "/heartbeat?peer_id=" + myId));
                req.ContentType = "application/json";
                req.Method = "GET";
                req.Headers["Peer-Type"] = "Client";

                using (HttpWebResponse response = req.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Console.WriteLine("Cannot connect to server");
                    }
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        var content = reader.ReadToEnd();
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            Console.Out.WriteLine("TESTTTTT Response contained empty body...");
                        }
                        else
                        {
                            Console.WriteLine("Response TESTTTTT: \r\n {0}", content);
                        }


                    }
                }
            }, null, TimeSpan.FromSeconds(heartBeatIntervalInSecs), TimeSpan.FromSeconds(heartBeatIntervalInSecs));
        }

        //public class MyScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
        //{
        //    private readonly VideoRendererWithGesture _view;


        //    public MyScaleListener(VideoRendererWithGesture view)
        //    {
        //        _view = view;
        //    }

        //    public override bool OnScale(ScaleGestureDetector detector)
        //    {
        //        var dist = detector.ScaleFactor;
        //        _view.navLocation[0] = _view.downLocation[0] + dist * _view.navTransform[0, 0];
        //        _view.navLocation[1] = _view.downLocation[1] + dist * _view.navTransform[0, 1];
        //        _view.navLocation[2] = _view.downLocation[2] + dist * _view.navTransform[0, 2];

        //        _view.navTransform[3, 0] = _view.navLocation[0];
        //        _view.navTransform[3, 1] = _view.navLocation[1];
        //        _view.navTransform[3, 2] = _view.navLocation[2];

        //        _view.ToBuffer();

        //        //_view._gestureOutput.Text = String.Format("{0} {1} {2} {3} \n {4} {5} {6} {7} \n {8} {9} {10} {11} \n {12} {13} {14} {15}", _view.navTransform[0, 0], _view.navTransform[0, 1], _view.navTransform[0, 2], _view.navTransform[0, 3], _view.navTransform[1, 0], _view.navTransform[1, 1], _view.navTransform[1, 2], _view.navTransform[1, 3], _view.navTransform[2, 0], _view.navTransform[2, 1], _view.navTransform[2, 2], _view.navTransform[2, 3], _view.navTransform[3, 0], _view.navTransform[3, 1], _view.navTransform[3, 2], _view.navTransform[3, 3]);

        //        return true;
        //    }
        //}

        //public class VideoRendererWithGesture : SurfaceViewRenderer//, View.IOnTouchListener
        //{
        //    float navHeading = 0;
        //    float navPitch = 0;
        //    public float[] navLocation = new float[] { 0, 0, 0 };
        //    bool isFingerDown = false;
        //    float fingerDownX;
        //    float fingerDownY;
        //    float downPitch = 0;
        //    float downHeading = 0;
        //    public float[] downLocation = new float[] { 0, 0, 0 };
        //    public float[,] navTransform;

        //    IOnMotionEventListener mListener;

        //    ScaleGestureDetector _scaleDetector;

        //    public VideoRendererWithGesture(Context context) : base(context)
        //    {
        //    }

        //    public VideoRendererWithGesture(Context context, IAttributeSet attrs) : base(context, attrs)
        //    {
        //    }

        //    public void SetEventListener(IOnMotionEventListener eventListener)
        //    {
        //        mListener = eventListener;
        //    }

        //    public void SetScaleGestureDetector(ScaleGestureDetector detector)
        //    {
        //        _scaleDetector = detector;
        //    }

        //    public override bool OnTouchEvent(MotionEvent e)
        //    {
        //        _scaleDetector.OnTouchEvent(e);
        //        if (e.PointerCount == 1)
        //        {
        //            switch (e.Action)
        //            {
        //                case MotionEventActions.Down:
        //                    isFingerDown = true;
        //                    fingerDownX = e.RawX;
        //                    fingerDownY = e.RawY;

        //                    downPitch = navPitch;
        //                    downHeading = navHeading;
        //                    downLocation[0] = navLocation[0];
        //                    downLocation[1] = navLocation[1];
        //                    downLocation[2] = navLocation[2];

        //                    break;
        //                case MotionEventActions.Move:
        //                    var dx = e.RawX - fingerDownX;
        //                    var dy = e.RawY - fingerDownY;

        //                    var dpitch = (float)0.005 * dy;
        //                    var dheading = (float)0.005 * dx;

        //                    navHeading = downHeading - dheading;
        //                    navPitch = downPitch + dpitch;
        //                    var localTransform = MatMultiply(MatRotateY(navHeading), MatRotateZ(navPitch));
        //                    navTransform = MatMultiply(MatTranslate(navLocation), localTransform);

        //                    ToBuffer();
        //                    //_gestureOutput.Text = String.Format("{0} {1} {2} {3} \n {4} {5} {6} {7} \n {8} {9} {10} {11} \n {12} {13} {14} {15}", navTransform[0, 0], navTransform[0, 1], navTransform[0, 2], navTransform[0, 3], navTransform[1, 0], navTransform[1, 1], navTransform[1, 2], navTransform[1, 3], navTransform[2, 0], navTransform[2, 1], navTransform[2, 2], navTransform[2, 3], navTransform[3, 0], navTransform[3, 1], navTransform[3, 2], navTransform[3, 3]);
        //                    break;
        //                case MotionEventActions.Up:
        //                    isFingerDown = false;
        //                    break;
        //            }
        //        }
        //        return true;
        //    }

        //    public void ToBuffer()
        //    {
        //        if (mListener == null)
        //        {
        //            return;
        //        }
        //        float[] eye = { navTransform[3, 0], navTransform[3, 1], navTransform[3, 2] };
        //        float[] lookat = { navTransform[3, 0] + navTransform[0, 0], navTransform[3, 1] + navTransform[0, 1], navTransform[3, 2] + navTransform[0, 2] };
        //        float[] up = { navTransform[1, 0], navTransform[1, 1], navTransform[1, 2] };

        //        string data = eye[0] + ", " + eye[1] + ", " + eye[2] + ", " +
        //                    lookat[0] + ", " + lookat[1] + ", " + lookat[2] + ", " +
        //                    up[0] + ", " + up[1] + ", " + up[2];

        //        var content = new JObject();
        //        content.Add("type", "camera-transform-lookat");
        //        content.Add("body", data);
        //        //var content = new StringContent(data, Encoding.UTF8, "camera-transform-lookat");
        //        Console.WriteLine(content.ToString(Newtonsoft.Json.Formatting.None));
        //        ByteBuffer byteBuffer = ByteBuffer.Wrap(Encoding.ASCII.GetBytes(content.ToString()));
        //        DataChannel.Buffer buffer = new DataChannel.Buffer(byteBuffer, false);
        //        mListener.SendTransofrm(buffer);

        //    }

        //    public interface IOnMotionEventListener
        //    {
        //        void SendTransofrm(DataChannel.Buffer server);
        //    }
        //}

        private class MotionEventListener : VideoRendererWithControls.IOnMotionEventListener
        {
            public void SendTransofrm(DataChannel.Buffer buffer)
            {
                if (inputChannel != null)
                {
                    inputChannel.Send(buffer);
                }
            }
        }

        private class PeerObserver : Java.Lang.Object, PeerConnection.IObserver
        {
            public new void Dispose()
            {
                throw new NotImplementedException();
            }

            public void OnAddStream(MediaStream stream)
            {
                Console.Write("In OnAddSTream");
                if (peerConnection == null)
                {
                    return;
                }
                if (stream.AudioTracks.Size() > 1 || stream.VideoTracks.Size() > 1)
                {
                    Console.Write("This stream ain't right::   " + stream);
                }
                if (stream.VideoTracks.Size() == 1)
                {
                    remoteVideoTrack = (VideoTrack)stream.VideoTracks.Get(0);
                    // TO DO: MAKE A RUNNABLE
                    remoteVideoTrack.SetEnabled(true);
                    remoteVideoTrack.AddRenderer(new VideoRenderer(remoteVideoRenderer));
                }
            }

            public void OnAddTrack(RtpReceiver p0, MediaStream[] p1)
            {
                Console.WriteLine(p0.ToString());
                Console.WriteLine(p1.ToString());
            }

            public void OnDataChannel(DataChannel p0)
            {
                Console.Write("added data channel)");
                p0.RegisterObserver(new DcObserver());
                inputChannel = p0;
            }

            public void OnIceCandidate(IceCandidate iceCand)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("sdpMLineIndex", iceCand.SdpMLineIndex.ToString());
                dict.Add("sdpMid", iceCand.SdpMid.ToString());
                dict.Add("candidate", iceCand.Sdp.ToString());

                SendToPeer(peerId, dict);
            }

            public void OnIceCandidatesRemoved(IceCandidate[] p0)
            {
                throw new NotImplementedException();
            }

            public void OnIceConnectionChange(PeerConnection.IceConnectionState p0)
            {
                Console.WriteLine(p0.ToString());
            }

            public void OnIceConnectionReceivingChange(bool p0)
            {
                throw new NotImplementedException();
            }

            public void OnIceGatheringChange(PeerConnection.IceGatheringState p0)
            {
                Console.WriteLine(p0.ToString());
            }

            public void OnRemoveStream(MediaStream p0)
            {
                throw new NotImplementedException();
            }

            public void OnRenegotiationNeeded()
            {
                
            }

            public void OnSignalingChange(PeerConnection.SignalingState p0)
            {
                Console.WriteLine(p0.ToString());
            }
        }

        private class DcObserver : Java.Lang.Object, IObserver
        {
            public void Dispose()
            {
                
            }

            public void OnBufferedAmountChange(long p0)
            {
                Console.WriteLine("Buffer Amount Changed");
            }

            public void OnMessage(DataChannel.Buffer buffer)
            {
                if (buffer.Binary)
                {
                    Console.WriteLine("Recieved binary message");
                    return;
                }
                ByteBuffer data = buffer.Data;
                byte[] bytes = new byte[data.Capacity()];
                data.Get(bytes);
                //string strData = new string(bytes);
            }

            public void OnStateChange()
            {
                Console.WriteLine("State has changed");
            }
        }

        private class SDPObserver : Java.Lang.Object, ISdpObserver
        {
            private Dictionary<string, string> descriptionData;

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public void OnCreateFailure(string p0)
            {
                throw new NotImplementedException();
            }

            public void OnCreateSuccess(SessionDescription origSdp)
            {
                /* This can be called on CreateAnswer Success or CreateOffer Success. */

                string description = origSdp.Description;

                SessionDescription sd;
                if (peerConnection != null)
                {
                    //We created the offer
                    if (isInitiator)
                    {
                        // we want to use H264
                        description.Replace("96 97 98 99", "100 96 98 102");

                        sd = new SessionDescription(SessionDescription.Type.Offer, description);
                        descriptionData = new Dictionary<string, string>
                        {
                            { "type", "offer" },
                            { "sdp", sd.Description.ToString() }
                        };

                        isInitiator = false;
                    }
                    // Creating an answer
                    else
                    {
                        sd = new SessionDescription(SessionDescription.Type.Answer, description);
                        Console.Write("creating answer");
                        
                        descriptionData = new Dictionary<string, string>
                        {
                            { "type", "answer" },
                            { "sdp", sd.Description.ToString() }
                        };

                    }

                    isLocal = true;
                    peerConnection.SetLocalDescription(sdpObserver, sd); 
                }
            }

            public void OnSetFailure(string p0)
            {
                throw new NotImplementedException();
            }

            public async void OnSetSuccess()
            {
                if (isLocal)
                {
                    await SendToPeer(peerId, descriptionData);
                    isLocal = false;
                }
                if (isRemote)
                {
                    isRemote = false;
                    // we just set a remote description
                    Console.WriteLine("Successfully set the remote description");
                }
            }
        }
    }
}