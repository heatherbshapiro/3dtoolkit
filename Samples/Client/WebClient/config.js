var pcConfigStatic = {
    "iceServers": [{
        "url": "turn:turnserver3dstreaming.centralus.cloudapp.azure.com:5349",
        "username": "user",
        "credential": "3Dtoolkit072017",
        "credentialType": "password"
    }],
    "iceTransportPolicy": "relay"
};

var pcConfigDynamic = {
    "iceServers": [{
        "url": "turn:turnserveruri:5349",
        "credentialType": "password"
    }],
    "iceTransportPolicy": "relay"
};


var defaultSignalingServerUrl = "http://localhost:3001"

var aadConfig = {
    clientID: 'clientid',
    authority: "https://login.microsoftonline.com/tfp/tenant.onmicrosoft.com/b2c_1_signup",
    b2cScopes: ["openid"],
};

var identityManagementConfig = {
    turnCredsUrl: "https://identitymanagementapi"
};

var heartbeatIntervalInMs = 5000;