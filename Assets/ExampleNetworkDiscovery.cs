using System;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode.Community.Discovery;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkManager))]
public class ExampleNetworkDiscovery : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    [Serializable]
    public class ServerFoundEvent : UnityEvent<IPEndPoint, DiscoveryResponseData>
    {
    };

    NetworkManager m_NetworkManager;
    
    [SerializeField]
    [Tooltip("If true NetworkDiscovery will make the server visible and answer to client broadcasts as soon as netcode starts running as server.")]
    bool m_StartWithServer = true;

    public string ServerName = "EnterName";

    public ServerFoundEvent OnServerFound;
    
    private bool m_HasStartedWithServer = false;

    public void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
    }

    public void Update()
    {
        if (m_StartWithServer && m_HasStartedWithServer == false && IsRunning == false)
        {
            if (m_NetworkManager.IsServer)
            {
                StartServer();
                m_HasStartedWithServer = true;
            }
        }
    }

    protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
    {
        Debug.Log($"Broadcast received from {sender.Address}");
        response = new DiscoveryResponseData()
        {
            ServerName = ServerName,
            Port = ((UnityTransport) m_NetworkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
        };
        return true;
    }

    protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
    {
        Debug.Log($"Server Found : {sender.Address}");
        OnServerFound.Invoke(sender, response);
    }

    public override void StartClient()
    {
        base.StartClient();
        Debug.Log($"Client Started. IsClient is {IsClient}, IsServer is {IsServer}");
    }

    public override void StartServer()
    {
        base.StartServer();
        Debug.Log($"Server Started. ");
    }

    public override void StopDiscovery()
    {
        base.StopDiscovery();
        Debug.Log($"Discovery Stopped, IsClient is {IsClient}, IsServer is {IsServer}");
    }
}