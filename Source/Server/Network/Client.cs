﻿using System.Net;
using System.Net.Sockets;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Network;

public class Client
{
    [NonSerialized] public TcpClient tcp;
    [NonSerialized] public NetworkStream networkStream;
    [NonSerialized] public StreamWriter streamWriter;
    [NonSerialized] public StreamReader streamReader;
    [NonSerialized] public bool disconnectFlag;

    public string uid;

    public string username = "Unknown";

    public string password;

    public string factionName;

    public bool hasFaction;

    public bool isAdmin;

    public bool isBanned;

    [NonSerialized] public Client inVisitWith;

    [NonSerialized] public bool isBusy;

    [NonSerialized] public bool inSafeZone;

    [NonSerialized] public List<string> runningMods = new List<string>();

    [NonSerialized] public List<string> allyPlayers = new List<string>();

    [NonSerialized] public List<string> enemyPlayers = new List<string>();

    [NonSerialized] public Task? DataTask;

    public string SavedIP { get; set; }

    public Client(TcpClient tcp)
    {
        if (tcp == null) return;
        else
        {
            this.tcp = tcp;
            networkStream = tcp.GetStream();
            streamWriter = new StreamWriter(networkStream);
            streamReader = new StreamReader(networkStream);

            SavedIP = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
        }
    }

    public void SendData(Packet packet)
    {
        while (isBusy) Thread.Sleep(100);

        try
        {
            isBusy = true;

            streamWriter.WriteLine(Serializer.SerializeToString(packet));
            streamWriter.Flush();

            isBusy = false;
        }
        catch { disconnectFlag = true; }
    }
}
