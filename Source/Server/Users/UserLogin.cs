using RimworldTogether.GameServer.Managers;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Users
{
    public class UserLogin
    {
        private readonly ChatManager chatManager;
        private readonly ClientManager clientManager;
        private readonly UserManager userManager;
        private readonly SaveManager saveManager;
        private readonly ServerOverallManager serverOverallManager;
        private readonly WorldManager worldManager;
        private readonly UserManager_Joinings userManager_Joinings;
        private readonly ModManager modManager;

        public UserLogin(
            ChatManager chatManager,
            ClientManager clientManager,
            UserManager userManager,
            SaveManager saveManager,
            ServerOverallManager serverOverallManager,
            WorldManager worldManager,
            UserManager_Joinings userManager_Joinings,
            ModManager modManager
            )
        {
            this.chatManager = chatManager;
            this.clientManager = clientManager;
            this.userManager = userManager;
            this.saveManager = saveManager;
            this.serverOverallManager = serverOverallManager;
            this.worldManager = worldManager;
            this.userManager_Joinings = userManager_Joinings;
            this.modManager = modManager;
        }

        public void TryLoginUser(Client client, Packet packet)
        {
            LoginDetailsJSON loginDetails = Serializer.SerializeFromString<LoginDetailsJSON>(packet.contents[0]);
            client.username = loginDetails.username;
            client.password = loginDetails.password;

            if (!userManager_Joinings.CheckWhitelist(client)) return;

            if (!userManager_Joinings.CheckLoginDetails(client, UserManager_Joinings.CheckMode.Login)) return;

            if (!userManager.CheckIfUserExists(client)) return;

            userManager.LoadDataFromFile(client);

            if (modManager.CheckIfModConflict(client, loginDetails)) return;

            if (userManager.CheckIfUserBanned(client)) return;

            RemoveOldClientIfAny(client);

            PostLogin(client);
        }

        private void PostLogin(Client client)
        {
            UserManager.SaveUserIP(client);

            userManager.SendPlayerRecount();

            serverOverallManager.SendServerOveralls(client);

            chatManager.SendMessagesToClient(client, ChatManager.defaultJoinMessages);

            if (WorldManager.CheckIfWorldExists())
            {
                if (SaveManager.CheckIfUserHasSave(client)) saveManager.LoadUserGame(client);
                else worldManager.SendWorldFile(client);
            }
            else worldManager.RequireWorldFile(client);
        }

        private void RemoveOldClientIfAny(Client client)
        {
            foreach (Client cClient in clientManager.Clients.ToArray())
            {
                if (cClient == client) continue;
                else
                {
                    if (cClient.username == client.username)
                    {
                        userManager_Joinings.SendLoginResponse(cClient, UserManager_Joinings.LoginResponse.ExtraLogin);
                    }
                }
            }
        }
    }
}
