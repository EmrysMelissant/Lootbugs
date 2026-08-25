using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Startup Settings")]
    [SerializeField] private bool autoStartAsHost = false;
    [SerializeField] private ushort defaultPort = 7777;

    public ushort DefaultPort => defaultPort;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (autoStartAsHost)
        {
            ConnectHost(defaultPort);
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.L))
        {
            Disconnect();
        }
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    public bool ConnectHost(ushort port = 7777)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            return false;
        }

        ConfigureTransport("127.0.0.1", port, "0.0.0.0");
        return NetworkManager.Singleton.StartHost();
    }

    public bool ConnectClient(string ip = "127.0.0.1", ushort port = 7777)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            return false;
        }

        string targetIp = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip.Trim();
        ConfigureTransport(targetIp, port, "0.0.0.0");
        return NetworkManager.Singleton.StartClient();
    }

    private void ConfigureTransport(string address, ushort port, string listenAddress)
    {
        if (NetworkManager.Singleton == null) return;

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(address, port, listenAddress);
        }
    }

    public static string GetLocalIPAddress()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    return endPoint.Address.ToString();
                }
            }
        }
        catch
        {
            // Fallback DNS network interface scan
            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // Fallback to loopback if no adapter found
            }
        }
        return "127.0.0.1";
    }
}
