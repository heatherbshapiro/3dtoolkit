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
using Newtonsoft.Json;
using Java.Util.Regex;

using Xamarin_Android.Droid.Resources;
using static Xamarin_Android.Droid.MatrixMath;
using Java.Nio;

namespace Xamarin_Android.Droid
{
    [Activity(Label = "Connect to Stream", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class VideoStream : Activity
    {
        static string serverUrl;
        static PeerConnection peerConnection;
        PeerConnectionFactory pcFactory;
        List<string> servers;
        ListView serverList;
        static string myId = "-1";
        static string peerId;
        static bool isInitiator;
        static SessionDescription localSdp;

        int heartBeatIntervalInSecs = 5;
        System.Threading.Timer timer;

        static HttpClient client;
        static VideoTrack remoteVideoTrack;
        static VideoRendererWithControls remoteVideoRenderer;
        MediaConstraints pcConstraints;
        MediaConstraints sdpMediaConstraints;
        static DataChannel inputChannel;
        PeerConnection.IObserver pcObserver;
        static ISdpObserver sdpObserver = new SDPObserver();

        ArrayAdapter<string> adapter;
        IEglBase eglBase;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            RequestWindowFeature(WindowFeatures.NoTitle);

            SetContentView(Resource.Layout.VideoStream);

            string connectedServers = Intent.GetStringExtra("serverList");
            serverUrl = Intent.GetStringExtra("serverUrl");

            string[] server = connectedServers.Split();
            servers = server.ToList();
            myId = servers[0].Split(',')[1];
            servers.RemoveAt(0);
            servers.RemoveAt(server.Count()-2);
            serverList = FindViewById<ListView>(Resource.Id.server_list);
            adapter = new ArrayAdapter<string>(this, Resource.Layout.VideoStream, Resource.Id.textItem, servers);
            serverList.Adapter = adapter;
            Button disconnectButton;
            BeginProcess();

            serverList.ItemClick += delegate (object sender, AdapterView.ItemClickEventArgs args)
            {
                string clicked = servers[args.Position].ToString();
                string peer = servers[args.Position].Split(',')[1];
                string serverName = servers[args.Position].Split(',')[0];
                Console.WriteLine("peername: " + serverName);

                JoinPeer(peer);
              
            };
        }

        public override void OnBackPressed()
        {
            Disconnect();
            base.OnBackPressed();
        }

        protected void BeginProcess()
        {
            StartHangingGet();
            StartHeartbeat();
            //UpdatePeerList(); 
        }

        /*
         * Disconnects the current PeerConnection and ends the activity. 
         */
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

            if (myId != "-1")
            {
                //Tell the server we are signing out
                var req = new HttpWebRequest(new Uri(serverUrl + "/sign_out?peer_id=" + myId));
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

            if (eglBase != null)
            {
                try
                {
                    eglBase.Release();
                }
                catch (RuntimeException e)
                {
                    Console.Write(e.ToString());
                }
            }

            PeerConnectionFactory.StopInternalTracingCapture();
            PeerConnectionFactory.ShutdownInternalTracer();
            Finish();
        }

        /*
         * Joins server selected from the list of peers
         * @param peer: string representing the server selected. 
         */
        public void JoinPeer(string peer)
        {
            CreatePeerConnection(peer);
            inputChannel = peerConnection.CreateDataChannel("inputDataChannel", new DataChannel.Init());
            inputChannel.RegisterObserver(new DcObserver());

            isInitiator = true;
            peerConnection.CreateOffer(sdpObserver, sdpMediaConstraints);
        }

        /*
         * Create a PeerConnection
         * @param peer: given peer to create connection with.
         */
        public void CreatePeerConnection(string peer)
        {
            /* Set this peer to the global variable to observers can access it. */
            peerId = peer;

            /* Supply the media constraints */
            pcConstraints = new MediaConstraints();
            pcConstraints.Optional.Add(new MediaConstraints.KeyValuePair("DtlsSrtpKeyAgreement", "true"));
            pcConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveAudio", "false"));
            pcConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveVideo", "true"));

            sdpMediaConstraints = new MediaConstraints();
            sdpMediaConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveAudio", "false"));
            sdpMediaConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveVideo", "true"));

            /* Create the Ice Server */
            PeerConnection.IceServer ice = new PeerConnection.IceServer(Config.turnServer, Config.username, Config.credential, Config.tlsCertPolicy);
            List<PeerConnection.IceServer> iceServers = new List<PeerConnection.IceServer> { ice };
            PeerConnection.RTCConfiguration rtcConfig = new PeerConnection.RTCConfiguration(iceServers);
            rtcConfig.IceTransportsType = PeerConnection.IceTransportsType.Relay;

            /* Create the observer */
            pcObserver = new PeerObserver();

            /* Initialize the PeerConnectionFactory*/
            PeerConnectionFactory.InitializeAndroidGlobals(this, false, true, true);

            pcFactory = new PeerConnectionFactory();
            IEglBase eglBase = EglBaseFactory.Create();
            Button disconnectButton;
            disconnectButton = new Button(this)
            {
                Text = "Disconnect"
            };
            disconnectButton.Click += delegate (object s, EventArgs e)
            {
                Disconnect();
            };

            var layout = new RelativeLayout(this);
            remoteVideoRenderer = new VideoRendererWithControls(this);
            remoteVideoRenderer.Init(eglBase.GetEglBaseContext(), null);
            remoteVideoRenderer.SetEventListener(new MotionEventListener());
            Window.AddFlags(WindowManagerFlags.DismissKeyguard | WindowManagerFlags.TurnScreenOn | WindowManagerFlags.ShowWhenLocked);
            remoteVideoRenderer.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.HideNavigation | (StatusBarVisibility)SystemUiFlags.Fullscreen | (StatusBarVisibility)SystemUiFlags.ImmersiveSticky;
            layout.AddView(remoteVideoRenderer);
            layout.AddView(disconnectButton);
            SetContentView(layout);

            /* Establish peer connection */
            peerConnection = pcFactory.CreatePeerConnection(rtcConfig, pcConstraints, pcObserver);
        }

        /*
         * Sends POST request to the server with data
         * @param data: content for message to send to peer
         * @param peer: peer to send message to
         */
        static async Task SendToPeer(string peer, Dictionary<string, string> data)
        {

            if (myId == "-1")
            {
                Console.Write("SendToPeer Not Connected");
                return;
            }
            if (peer == myId)
            {
                Console.Write("SendToPeer Can't send a message to yoself!");
                return;
            }

            client = new HttpClient();

            var json = JsonConvert.SerializeObject(data);
            JObject dataToSend = JObject.Parse(json);
            var content = new StringContent(json, Encoding.UTF8, "text/plain");
            content.Headers.Add("Peer-Type", "Client");
            var req = new Uri(serverUrl + "/message?peer_id=" + myId + "&to=" + peer);
            HttpResponseMessage response = null;
            response = await client.PostAsync(req, content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Successfully sent message to peer: {0}", response.Content.ToString());
            }
            else
            {
                Console.Write("RESPONSE FROM SENT MESSAGE: " + response.StatusCode + "   " + response.ReasonPhrase);
                Console.Write("this is the request message" + response.RequestMessage);
                string x = await content.ReadAsStringAsync();
                Console.Write("this was the content" + x);
            }
        }


        /*
         * Handles message from the server: offer, answer, or adding ice candidate.
         * 
         * @param data is the JSON response from the server
         * @sender is the peer that sent the message
         */
        public void HandlePeerMessage(string sender, JObject data)
        {
            Console.WriteLine("Received: " + data.ToString());

            bool dataType = data.GetValue("type") == null ? false : true;
            if (dataType)
            {
                if (data.GetValue("type").ToString().Equals("offer"))
                {
                    SessionDescription sdp = new SessionDescription(SessionDescription.Type.Offer, data.GetValue("sdp").ToString());
                    CreatePeerConnection(sender);
                    isInitiator = false;
                    peerConnection.SetRemoteDescription(sdpObserver, sdp);
                    peerConnection.CreateAnswer(sdpObserver, sdpMediaConstraints);
                }
                else if (data.GetValue("type").ToString().Equals("answer"))
                {
                    SessionDescription sdp = new SessionDescription(SessionDescription.Type.Answer, data.GetValue("sdp").ToString());
                    Console.Write("Got answer");
                    peerConnection.SetRemoteDescription(sdpObserver, sdp);
                }
            }
            else
            {
                string iceSdp = (string)data.SelectToken("candidate");
                string sdpMid = (string)data.SelectToken("sdpMid");
                int sdpMidLineIndex = Int32.Parse((string)data.SelectToken("sdpMLineIndex"));
                IceCandidate newCandidate = new IceCandidate(sdpMid, sdpMidLineIndex, iceSdp);
                Console.Write("Adding ICE candiate " + newCandidate.ToString());
                peerConnection.AddIceCandidate(newCandidate);
            }
        }

        /** 
         * Updates the list view with updated server list. 
         */
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

        /**
         * Handles adding new servers to the list of servers
         * @param: newly available peer's server returned from polling.
         */
        public void HandleServerNotification(string server)
        {
            string[] info = server.Trim().Split(',');
            if (Int32.Parse(info[2]) != 0)
            {
                servers.Add(server);
            }
            UpdatePeerList();
        }

        /**
         * Handles two cases:
         * 1) When a new server appears and should be added to the list of servers.
         * 2) When the peer sends a message: offer, answer, or ice candidate. 
         * This loops on request timeout. 
         */
        private async Task StartHangingGet()
        {
            // Create an HTTP web request using the URL:
            Uri urlString = new Uri(serverUrl + "/wait?peer_id=" + myId);
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
                catch (WebException err)
                {
                    if (err.Status == WebExceptionStatus.Timeout)
                    {
                        StartHangingGet();
                    }
                    Console.WriteLine("Error" + err);
                }
                catch (System.Exception e)
                {
                    Console.WriteLine("Server Error +  " + e);
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

        /**
         * Sends heartbeat request to server at a regular interval. 
         */
        public void StartHeartbeat()
        {
            timer = new System.Threading.Timer((e) =>
            {

                var req = new HttpWebRequest(new Uri(serverUrl + "/heartbeat?peer_id=" + myId));
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
                            Console.Out.WriteLine("Heartbeat esponse contained empty body...");
                        }
                        else
                        {
                            Console.WriteLine("Heartbeat: \r\n {0}", content);
                        }


                    }
                }
            }, null, TimeSpan.FromSeconds(heartBeatIntervalInSecs), TimeSpan.FromSeconds(heartBeatIntervalInSecs));
        }

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

                if (localSdp != null)
                {
                    Console.Write("we already have an sdp. error");
                    return;
                }
                string description = origSdp.Description;
                SessionDescription sd;
                description.Replace("96 97 98 99", "100 96 98 102");

                SessionDescription.Type type = (isInitiator) ? SessionDescription.Type.Offer : SessionDescription.Type.Answer;

                sd = new SessionDescription(type, description);
                localSdp = origSdp;

                if (peerConnection != null)
                {
                    peerConnection.SetLocalDescription(sdpObserver, sd);
                }

            }

            public void OnSetFailure(string p0)
            {
                throw new NotImplementedException();
            }

            public async void OnSetSuccess()
            {
                if (peerConnection == null || localSdp == null)
                {
                    return;
                }
                if (isInitiator)
                {
                    if (peerConnection.RemoteDescription == null)
                    {
                        // Just set our localSDP, sending off our offer. 
                        descriptionData = new Dictionary<string, string>
                        {
                            { "type", "offer" },
                            { "sdp", localSdp.Description}
                        };
                        await SendToPeer(peerId, descriptionData);
                    }

                    else
                    {
                        Console.Write("onRemoteSDPSuccess");
                    }
                }

                else
                {
                    if (peerConnection.LocalDescription != null)
                    {
                        // we just set our local SDP after creating our answer, send it
                        descriptionData = new Dictionary<string, string>
                        {
                            { "type", "answer" },
                            { "sdp", localSdp.Description}
                        };
                        await SendToPeer(peerId, descriptionData);
                    }
                    else
                    {
                        Console.Write("onRemoteSDPSuccess");
                    }
                }
            }
        }
    }
}