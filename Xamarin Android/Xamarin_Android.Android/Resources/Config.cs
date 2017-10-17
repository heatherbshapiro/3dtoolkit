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

namespace Xamarin_Android.Droid.Resources
{
    public class Config
    {
            public static String signalingServer = "https://3dtoolkit-signaling-server.azurewebsites.net:443";
            public static String turnServer = "turn:turnserver3dstreaming.centralus.cloudapp.azure.com:5349";
            public static String username = "user";
            public static String credential = "3Dtoolkit072017";
            public static PeerConnection.TlsCertPolicy tlsCertPolicy = PeerConnection.TlsCertPolicy.TlsCertPolicyInsecureNoCheck;
    }
}
