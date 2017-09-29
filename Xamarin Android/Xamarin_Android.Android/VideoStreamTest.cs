using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Json;

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
using System.Net;
using System.Threading.Tasks;
using System.IO;

namespace Xamarin_Android.Droid
{
    [Activity(Label = "VideoStreamTest")]
    public class VideoStreamTest : Activity
    {
        Java.Lang.JavaSystem.loadLibrary("lib/armeabi-v7a/libjingle_peerconnection_so.so");
        
        string AUDIO_CODEC_PARAM_BITRATE = "maxaveragebitrate";
        string AUDIO_ECHO_CANCELLATION_CONSTRAINT = "googEchoCancellation";
        string AUDIO_AUTO_GAIN_CONTROL_CONSTRAINT = "googAutoGainControl";
        string AUDIO_HIGH_PASS_FILTER_CONSTRAINT = "googHighpassFilter";
        string AUDIO_NOISE_SUPPRESSION_CONSTRAINT = "googNoiseSuppression";

        static PeerConnection peerConnection;
        PeerConnectionFactory pcFactory;
        static string myId;
        static string peerId; //used when we initiate the message
        bool heartBeatTimerIsRunning = false;
        int heartBeatIntervalInSecs = 5;
        System.Timers.Timer heartBeatTimer;
        int messageCounter;

        public const string url = "https://3dtoolkit-signaling-server.azurewebsites.net";

        VideoTrack videoTrack;
        //SurfaceViewRenderer remoteVideoView;

        DataChannel inputDataChannel;

        MediaConstraints pcConstraints;
        MediaConstraints videoConstraints;
        MediaConstraints audioConstraints;
        MediaConstraints sdpMediaConstraints;
        MediaStream audioMediaStream;
        DataChannel inputChannel;
        Context context;

        PeerConnection.IObserver observer;
        static ISdpObserver sdpObserver;

        static SessionDescription localSdp;

        static bool isInitiator;
        ListView serverList;
        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            string ServerList = Intent.GetStringExtra("server_list");
            myId = Intent.GetStringExtra("myId");
            Console.Write("MYID     " + myId.ToString());
            string[] server = ServerList.Split();
            List<string> servers = server.ToList();
            servers.RemoveAt(0);

            SetContentView(Resource.Layout.VideoStreamTest);
            serverList = FindViewById<ListView>(Resource.Id.server_list);

            serverList.Adapter = new ArrayAdapter<string>(this, Resource.Layout.VideoStreamTest, Resource.Id.textItem, servers); ;
            Console.Write("SERVERLIST   " + servers);

            serverList.ItemClick += delegate (object sender, AdapterView.ItemClickEventArgs args)
            {
                string clicked = servers[args.Position].ToString();
                peerId = servers[args.Position].ToString().Split(',')[1];
                Console.WriteLine("peerid + " + peerId);
                joinPeer();
            };

            localSdp = null;
            peerConnection = null;

            SurfaceView remoteRender = FindViewById<SurfaceView>(Resource.Id.video_view);
            await Task.Delay(2000);

            BeginProcess();
        }

        protected void BeginProcess()
        {
            StartHangingGet();
            Console.Write("IM DONE THIS TASK");
            StartHeartBeat();
        }

        private async Task StartHangingGet()
        {
            // Create an HTTP web request using the URL:
            Uri urlString = new Uri(url + "/wait?peer_id=" + myId);
            //Uri urlString = new Uri("https://jsonplaceholder.typicode.com/posts/1");
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
                    // Get a stream representation of the HTTP web response:
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
            Console.Write("XXXXXXX IN HANGING GET CALLBACK NOW");

            string data;
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                data = reader.ReadToEnd();
            }

            string sender = response.GetResponseHeader("Pragma");
            if (sender == myId)
            {
                //HandleSeverNotification(data);
            } else
            {
                HandlePeerMessage(sender, data);
            }

            if (myId != "-1")
            {
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

            //Uri urlString = new Uri(url + "/heartbeat?peer_id=" + myId.ToString());
            //HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(urlString);
            //request.Method = "GET";
            //request.Headers["Peer-Type"] = "Client";
            //using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            //{
            //    //discard response.
            //}
        }

        public void SendToPeer(string peerId, string data)
        {
            if (myId == "-1")
            {
                System.Diagnostics.Debug.WriteLine("Not connected");
                return;
            }

            if (peerId == myId)
            {
                System.Diagnostics.Debug.WriteLine("Can't send message to onself");
                return;
            }

            var req = new HttpWebRequest(new Uri(url + "/message?peer_id=" + myId + "&to=" + peerId));
            req.ContentType = "text/plain";
            req.Method = "POST";
            req.Headers["Peer-Type"] = "Client";
            //add body

            using (HttpWebResponse response = req.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine("Cannot connect to server");
                }
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                    //if (string.IsNullOrWhiteSpace(content))
                    //{
                    //    Console.Out.WriteLine("TESTTTTT Response contained empty body...");
                    //}
                    //else
                    //{
                    //    Console.WriteLine("Response TESTTTTT: \r\n {0}", content);
                    //}


                }
            }
        }

        public void HandlePeerMessage(string sender, string data)
        {
            messageCounter += 1;
            string str = "Message from " + sender + ":" + data;

            Console.WriteLine("Received " + data);
        }

        public void HandleServerNotification()
        {

        }

        public void joinPeer()
        {
            createPeerConnection();

            inputChannel = peerConnection.CreateDataChannel("inputDataChannel", new DataChannel.Init());
            inputChannel.RegisterObserver(new DcObserver());

            sdpObserver = new SDPObserver();

            isInitiator = true;
            peerConnection.CreateOffer(sdpObserver, sdpMediaConstraints);
            //SDPObserver will handle the response from CreateOffer. THen completion handler calls:
        }

        public void createPeerConnection()
        {

            /* Create the Ice Server */
            PeerConnection.IceServer ice = new PeerConnection.IceServer("turnserver3dstreaming.centralus.cloudapp.azure.com:5349", "user", "3Dtoolkit072017");
            //PeerConnection.IceTransportsType type = PeerConnection.IceTransportsType.Relay;
            List<PeerConnection.IceServer> servers = new List<PeerConnection.IceServer>();
            servers.Add(ice);
            PeerConnection.RTCConfiguration rtcConfig = new PeerConnection.RTCConfiguration(servers);
            rtcConfig.IceTransportsType = PeerConnection.IceTransportsType.Relay;

            /* Supply the media constraints */
            MediaConstraints constraints = createMediaConstraints();

            /* Create the observer */
            observer = new PeerObserver();

            /* Initialize the PeerConnectionFactory*/
            if (!PeerConnectionFactory.InitializeAndroidGlobals(this, true, false, false))
            {
                Console.WriteLine("initialize returns false!");
            }
            else
            {
                //PeerConnectionFactory.InitializeAndroidGlobals(this, true);
                pcFactory = new PeerConnectionFactory();

                /* Create Local Media */
                audioMediaStream = pcFactory.CreateLocalMediaStream("MARGARET");
                audioMediaStream.AddTrack(pcFactory.CreateAudioTrack(
                    "AUDIO_ID_FILLIN",
                    pcFactory.CreateAudioSource(audioConstraints)));

                /* Video Renderering Media Will Go Here. */


                /* Establish peer connection */

                //rtcConfig.TcpCandidatePolicy = PeerConnection.TcpCandidatePolicy.Disabled;
                //rtcConfig.BundlePolicy = PeerConnection.BundlePolicy.Maxbundle;
                //rtcConfig.RtcpMuxPolicy = PeerConnection.RtcpMuxPolicy.Require;
                //rtcConfig.ContinualGatheringPolicy = PeerConnection.ContinualGatheringPolicy.GatherContinually;
                //// Use ECDSA encryption.
                //rtcConfig.KeyType = PeerConnection.KeyType.Ecdsa;

                peerConnection = pcFactory.CreatePeerConnection(rtcConfig, constraints, observer);
                peerConnection.AddStream(audioMediaStream);
            }
            
        }

        public MediaConstraints createMediaConstraints()
        {

            /* Create Media Constraints */
            pcConstraints = new MediaConstraints();
            pcConstraints.Optional.Add(new MediaConstraints.KeyValuePair("DtlsSrtpKeyAgreement", "true"));

            audioConstraints = new MediaConstraints();
            sdpMediaConstraints = new MediaConstraints();
            sdpMediaConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveAudio", "false"));
            sdpMediaConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair("OfferToReceiveVideo", "true"));

            // Turn Audio Processing off
            audioConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair(
                AUDIO_ECHO_CANCELLATION_CONSTRAINT, "false"));
            audioConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair(
                AUDIO_AUTO_GAIN_CONTROL_CONSTRAINT, "false"));
            audioConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair(
                AUDIO_HIGH_PASS_FILTER_CONSTRAINT, "false"));
            audioConstraints.Mandatory.Add(new MediaConstraints.KeyValuePair(
                AUDIO_NOISE_SUPPRESSION_CONSTRAINT, "false"));

            return pcConstraints; // WHAT MEDIA CONSTRAINTS SHOULD BE RETURNED HERE ACTUALLY? apparently should contain receive video/audio

        }

        private class PeerObserver : Java.Lang.Object, PeerConnection.IObserver
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public void OnAddStream(MediaStream p0)
            {
                Console.Write("In OnAddSTream");
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
                if (localSdp != null)
                {
                    System.Diagnostics.Debug.WriteLine("Multiple SDP created");
                    return;
                }
                string sdpDescription = origSdp.Description;
                // we want to use H264
                sdpDescription.Replace("96 98 100 102", "100 96 98 102"); // WHAT SHOULD THIS LOOK LIKE?

                SessionDescription.Type sdpType = SessionDescription.Type.FromCanonicalForm(origSdp.GetType().ToString());
                // make sure these types are consistent... could just be offer?

                //Recreate the new offer object
                SessionDescription sdp = new SessionDescription(sdpType, sdpDescription);
                System.Diagnostics.Debug.WriteLine(origSdp.GetType().ToString());

                localSdp = sdp;
                if (peerConnection != null)
                {
                    //set local description
                    peerConnection.SetLocalDescription(sdpObserver, sdp);
                }
            }

            public void OnSetFailure(string p0)
            {
                throw new NotImplementedException();
            }

            public void OnSetSuccess()
            {

                if (VideoStreamTest.myId == "-1")
                {
                    System.Diagnostics.Debug.WriteLine("Not connected");
                    return;
                }

                if (VideoStreamTest.peerId == VideoStreamTest.myId)
                {
                    System.Diagnostics.Debug.WriteLine("Can't send message to onself");
                    return;
                }

                Uri urlString = new Uri(url + "/message?peer_id=" + VideoStreamTest.myId + "&to=" + VideoStreamTest.peerId);
                var request = new HttpWebRequest(urlString);
                request.Method = "GET";
                request.ContentType = "text/plain";
                request.Headers["Peer-Type"] = "Client";

                //if (isInitiator)
                //{
                //    // We created the offer and set localSDP, now after receiving answer set remote SDP.
                //    if (peerConnection.RemoteDescription == null)
                //    {
                //        // we have just set out local SDP so time to send it

                //    }
                //    else
                //    {
                //        // we have just set our remote description so drain remote and send local ICE candidates.
                //        if (peerConnection.LocalDescription != null)
                //        {
                //            //we've just set our local sdp so time to send it.
                //        }
                //        else
                //        {
                //            //we just set remote sdp - nothing to do for now. 
                //        }

                //    }
                // }
            }
        }
    }
}