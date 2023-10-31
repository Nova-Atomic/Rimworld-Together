using System.Xml;
using Microsoft.Extensions.Logging;

namespace RimworldTogether.GameServer.Misc
{
    public class XmlParser
    {
        private readonly ILogger<XmlParser> logger;

        public XmlParser(ILogger<XmlParser> logger)
        {
            this.logger = logger;
        }

        public string[] ParseDataFromXML(string xmlPath, string elementName)
        {
            List<string> result = new List<string>();

            try
            {
                XmlReader reader = XmlReader.Create(xmlPath);
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == elementName)
                    {
                        result.Add(reader.ReadElementContentAsString());
                    }
                }

                reader.Close();

                return result.ToArray();
            }
            catch (Exception e) { logger.LogError(e, $"[Error] > Failed to parse mod at '{xmlPath}'. Exception: {e}"); }

            return result.ToArray();
        }
    }
}
