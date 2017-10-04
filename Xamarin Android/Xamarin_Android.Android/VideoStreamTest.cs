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

namespace Xamarin_Android.Droid
{
    [Activity(Label = "VideoStreamTest")]
    public class VideoStreamTest : Activity
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
        int messageCounter;

        static HttpClient client; 

        static VideoTrack remoteVideoTrack;
        //static SurfaceViewRenderer fullscreenRenderer;
        //private EglBase rootEglBase = EglBase.Create();

        MediaConstraints pcConstraints;
        //MediaConstraints videoConstraints;
        //MediaConstraints audioConstraints;
        MediaStream audioMediaStream;
        MediaConstraints sdpMediaConstraints;
        DataChannel inputChannel;

        PeerConnection.IObserver pcObserver;
        static ISdpObserver sdpObserver;

        static bool isInitiator;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Java.Lang.JavaSystem.LoadLibrary("jingle_peerconnection_so");

            SetContentView(Resource.Layout.VideoStreamTest);

            /* TO DO: Initialize Video Tracks/Renderin. Errors with SurfaceViewRenderer */
            //fullscreenRenderer = FindViewById<SurfaceViewRenderer>(Resource.Id.video_view);
            //fullscreenRenderer.Init(rootEglBase.EglBaseContext, null);

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

            serverList.Adapter = new ArrayAdapter<string>(this, Resource.Layout.VideoStreamTest, Resource.Id.textItem, servers); ;

            BeginProcess();

            serverList.ItemClick += delegate (object sender, AdapterView.ItemClickEventArgs args)
            {
                string clicked = servers[args.Position].ToString();
                string peer = servers[args.Position].Split(',')[1];
                string serverName = servers[args.Position].Split(',')[0];
                Console.WriteLine("peername: " + serverName);

                JoinPeer(peer);
            };

            //peerConnection = null;

        }

        protected void BeginProcess()
        {
            StartHangingGet();
            Console.Write("IM DONE THIS TASK");
            StartHeartBeat();
            UpdatePeerList(); 
        }

        public void JoinPeer(string peer)
        {
            CreatePeerConnection(peer);

            inputChannel = peerConnection.CreateDataChannel("inputDataChannel", new DataChannel.Init());
            inputChannel.RegisterObserver(new DcObserver());

            sdpObserver = new SDPObserver();

            isInitiator = true;
            peerConnection.CreateOffer(sdpObserver, sdpMediaConstraints);
            //SDPObserver will handle the response from CreateOffer. THen completion handler calls:
        }

        public void CreatePeerConnection(string peer)
        {
            /* Set this peer to the global variable to observers can access it. */
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
            PeerConnection.IceServer ice = new PeerConnection.IceServer("turnserver3dstreaming.centralus.cloudapp.azure.com:5349", "user", "3Dtoolkit072017", PeerConnection.TlsCertPolicy.TlsCertPolicyInsecureNoCheck);
            //PeerConnection.IceTransportsType type = PeerConnection.IceTransportsType.Relay;
            //List<PeerConnection.IceServer> iceServers = new List<PeerConnection.IceServer>();
            //iceServers.Add(ice);
            List<PeerConnection.IceServer> iceServers = new List<PeerConnection.IceServer> { ice };

            //TESTED USING AN RTCONFIG INSTEAD OF SERVERS LIST 
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

        static async void SendToPeer(string peer, Dictionary<string, string> data)
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
            JObject dataToSend = (JObject)json;
            var content = new StringContent(dataToSend.ToString(), Encoding.UTF8);
            content.Headers.Add("Peer-Type", "Client");
            content.Headers.Add("Content-Type", "text/plain");
            var req = new Uri(url + "/message?peer_id=" + myId + "&to=" + peer);
            HttpResponseMessage response = null;
            response = await client.PostAsync(req, content);
            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("Successfully sent message to peer");
            }
        }

        public void HandlePeerMessage(string sender, JObject data)
        {
            messageCounter += 1;
            string str = "Message from " + otherPeers.GetValueOrDefault(sender) + ":" + data.ToString();

            Console.WriteLine("Received " + data.ToString());

            if (data.GetValue("type").Equals("offer"))
            {
                SessionDescription sdp = new SessionDescription(SessionDescription.Type.Offer, data.GetValue("sdp").ToString());
                CreatePeerConnection(sender);
                //isRemote = true;
                VideoStreamTest.peerConnection.SetRemoteDescription(sdpObserver, sdp);
                peerConnection.CreateAnswer(sdpObserver, sdpMediaConstraints);
            }
            else if (data.GetValue("type").Equals("answer") || data.GetValue("type").Equals("pranswer"))
            {
                SessionDescription sdp = new SessionDescription(SessionDescription.Type.Answer, data.GetValue("sdp").ToString());
                peerConnection.SetRemoteDescription(sdpObserver, sdp);
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
                foreach (KeyValuePair<string, string> peer in otherPeers)
                {
                    servers.Add(peer.Value);
                    Console.Write("data set change");
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
                jsonObj = JObject.Parse(data);
            }

            string sender = response.GetResponseHeader("Pragma");

            if (sender == myId)
            {
                HandleServerNotification(jsonObj.GetValue("Server").ToString());
            }
            else
            {
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
            var timer = new System.Threading.Timer((e) =>
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
                    // remoteVideoTrack.SetEnabled(true);
                    // remoteVideoTrack.AddRenderer(new VideoRenderer(fullscreenRenderer));
                }
            }

            public void OnAddTrack(RtpReceiver p0, MediaStream[] p1)
            {
                throw new NotImplementedException();
            }

            public void OnDataChannel(DataChannel p0)
            {
                Console.Write("added data channel)");
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
        }

        private class DcObserver : Java.Lang.Object, DataChannel.IObserver
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public void OnBufferedAmountChange(long p0)
            {
                throw new NotImplementedException();
            }

            public void OnMessage(DataChannel.Buffer p0)
            {
                throw new NotImplementedException();
            }

            public void OnStateChange()
            {
                throw new NotImplementedException();
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
                //TO DO : FIX LOCAL AND REMOTE OBSERVERS (MAKE TWO OF THEM) 

                string description = origSdp.Description;
                // we want to use H264
                description.Replace("96 98 100 102", "100 96 98 102");

                //Annoying conversion from System.Type to SessionDescription.Type (HOPEFULLY CAN CHANGE)
                SessionDescription.Type sdpType = SessionDescription.Type.FromCanonicalForm(origSdp.GetType().ToString());

                //Recreate the new offer object
                SessionDescription sdp = new SessionDescription(sdpType, description);

                if (peerConnection != null)
                {
                    // We created the offer
                    if (isInitiator)
                    {
                        descriptionData = new Dictionary<string, string>();
                        descriptionData.Add("type", "offer");
                        descriptionData.Add("sdp", sdp.Description.ToString());
                        isInitiator = false;
                    }
                    // 
                    else
                    {
                        Console.Write("creating answer");
                        SessionDescription.Type answerType = null;
                        switch (sdpType.ToString()) {
                            case ("offer"):
                                answerType = SessionDescription.Type.Offer;
                                break;
                            case ("answer"):
                            case ("pranswer"):
                                answerType = SessionDescription.Type.Answer;
                                break;
                        }

                        descriptionData = new Dictionary<string, string>();
                        descriptionData.Add("type", answerType.ToString());
                        descriptionData.Add("sdp", sdp.Description.ToString());
                        
                    }

                    peerConnection.SetLocalDescription(sdpObserver, sdp); 
                }
            }

            public void OnSetFailure(string p0)
            {
                throw new NotImplementedException();
            }

            public async void OnSetSuccess()
            {
                SendToPeer(peerId, descriptionData);
            }
        }
    }
}