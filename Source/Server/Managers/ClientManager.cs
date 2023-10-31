using RimworldTogether.GameServer.Network;

namespace RimworldTogether.GameServer.Managers
{
    public class ClientManager
    {
        private readonly List<Client> clients;

        public IEnumerable<Client> Clients => clients;

        public int ClientCount => clients.Count;

        public ClientManager()
        {
            clients = new List<Client>();
        }

        public void AddClient(Client client)
        {
            clients.Add(client);
        }

        public void RemoveClient(Client client)
        {
            clients.Remove(client);
        }
    }
}
