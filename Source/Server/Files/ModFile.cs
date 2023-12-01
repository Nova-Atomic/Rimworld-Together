using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimworldTogether.GameServer.Files
{
    [Serializable]
    public class ModFile
    {
        public string name;

        public string packageID;

        public string hash;

        public byte[] data;
    }
}
