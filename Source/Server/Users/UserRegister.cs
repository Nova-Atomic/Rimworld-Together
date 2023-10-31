using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Managers;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Users
{
    public class UserRegister
    {
        private readonly ILogger<UserRegister> logger;
        private readonly UserManager userManager;
        private readonly UserManager_Joinings userManager_Joinings;

        public UserRegister(
            ILogger<UserRegister> logger,
            UserManager userManager,
            UserManager_Joinings userManager_Joinings)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.userManager_Joinings = userManager_Joinings;
        }

        public void TryRegisterUser(Client client, Packet packet)
        {
            LoginDetailsJSON registerDetails = Serializer.SerializeFromString<LoginDetailsJSON>(packet.contents[0]);
            client.username = registerDetails.username;
            client.password = registerDetails.password;

            if (!userManager_Joinings.CheckLoginDetails(client, UserManager_Joinings.CheckMode.Register)) return;

            if (TryFetchAlreadyRegistered(client)) return;
            else
            {
                UserFile userFile = new UserFile();
                userFile.uid = GetNewUIDForUser(client);
                userFile.username = client.username;
                userFile.password = client.password;

                try
                {
                    UserManager.SaveUserFile(client, userFile);
                    userManager_Joinings.SendLoginResponse(client, UserManager_Joinings.LoginResponse.RegisterSuccess);
                    logger.LogInformation($"[Registered] > {client.username}");
                }

                catch
                {
                    userManager_Joinings.SendLoginResponse(client, UserManager_Joinings.LoginResponse.RegisterError);
                    return;
                }
            }
        }

        private bool TryFetchAlreadyRegistered(Client client)
        {
            string[] existingUsers = Directory.GetFiles(Program.usersPath);

            foreach (string user in existingUsers)
            {
                UserFile existingUser = Serializer.SerializeFromFile<UserFile>(user);
                if (existingUser.username.ToLower() != client.username.ToLower()) continue;
                else
                {
                    userManager_Joinings.SendLoginResponse(client, UserManager_Joinings.LoginResponse.RegisterInUse);
                    return true;
                }
            }

            return false;
        }

        private static string GetNewUIDForUser(Client client)
        {
            return Hasher.GetHash(client.username);
        }
    }
}
