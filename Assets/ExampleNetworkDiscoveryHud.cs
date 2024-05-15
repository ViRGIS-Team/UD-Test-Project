using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode.Community.Discovery;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Net.NetworkInformation;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

[RequireComponent(typeof(ExampleNetworkDiscovery))]
[RequireComponent(typeof(NetworkManager))]
public class ExampleNetworkDiscoveryHud : MonoBehaviour
{
    [SerializeField, HideInInspector]
    ExampleNetworkDiscovery m_Discovery;
    
    NetworkManager m_NetworkManager;

    Dictionary<IPEndPoint, DiscoveryResponseData> discoveredServers = new ();

    public Vector2 DrawOffset = new Vector2(10, 210);

    void Awake()
    {
        m_Discovery = GetComponent<ExampleNetworkDiscovery>();
        m_NetworkManager = GetComponent<NetworkManager>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (m_Discovery == null) // This will only happen once because m_Discovery is a serialize field
        {
            m_Discovery = GetComponent<ExampleNetworkDiscovery>();
            UnityEventTools.AddPersistentListener(m_Discovery.OnServerFound, OnServerFound);
            Undo.RecordObjects(new Object[] { this, m_Discovery}, "Set NetworkDiscovery");
        }
    }
#endif

    void OnServerFound(IPEndPoint sender, DiscoveryResponseData response)
    {
        discoveredServers[sender] = response;
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(DrawOffset, new Vector2(300, 200)));
        ClientSearchGUI();
        

        GUILayout.EndArea();
    }

    void ClientSearchGUI()
    {
        if (m_Discovery.IsServer)
        {
            if (GUILayout.Button("Stop Server"))
            {
                m_Discovery.StopDiscovery();
                m_NetworkManager.Shutdown();
            }
        }
        else
        {
            if (GUILayout.Button("Start Server"))
            {
                ushort port = 0;
                try
                {
                    IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                    IPEndPoint[] UDPendpoints = properties.GetActiveUdpListeners();
                    for (int i = 0; i < 10; i++)
                    {
                        port = (ushort)(7770 + i);
                        if (Array.Find<IPEndPoint>(UDPendpoints, ep =>
                        {
                            return ep.Port == port;
                        }) == null) break;
                    }
                }
                catch (NotImplementedException)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            port = (ushort)(7770 + i);
                            UdpClient m_Client = new UdpClient(port);
                            m_Client.Dispose();
                        }
                        catch (Exception e)
                        {
                            // do nothing - assuming this is a port clash
                            continue;
                        }
                        // if we get here - it worked
                        break;
                    }
                }
                UnityTransport transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
                transport.SetConnectionData("0.0.0.0", port);
                m_NetworkManager.StartServer();
                m_Discovery.StartServer();
            }
        }
        if (m_Discovery.IsClient)
        {
            if (GUILayout.Button("Stop Client Discovery"))
            {
                m_Discovery.StopDiscovery();
                m_NetworkManager.Shutdown();
                discoveredServers.Clear();
            }
            
            if (GUILayout.Button("Refresh List"))
            {
                discoveredServers.Clear();
                m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
            }
            
            GUILayout.Space(40);
            
            foreach (var discoveredServer in discoveredServers)
            {
                if (GUILayout.Button(
                    $"{discoveredServer.Value.ServerName}[{discoveredServer.Key.Address}:{discoveredServer.Key.Port}]"
                    )
                )
                {
                    UnityTransport transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
                    transport.SetConnectionData(discoveredServer.Key.Address.ToString(), discoveredServer.Value.Port);
                    m_NetworkManager.StartClient();
                }
            }
        }
        else
        {
            if (GUILayout.Button("Discover Servers"))
            {
                m_Discovery.StartClient();
                m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
            }
        }
    }
}
