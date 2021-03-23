using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeGrok.Client
{
    public static class ClientConfigProvider
    {
        private const string DefaultRemote = "https://localhost:5001/";

        public static async Task<ClientConfig> GetClientConfigAsync()
        {
            var configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FreeGrok");
            var configFile = Path.Combine(configDirectory, "config.json");
            if (!File.Exists(configFile))
            {
                if (!Directory.Exists(configDirectory))
                {
                    Directory.CreateDirectory(configDirectory);
                }
                await File.WriteAllTextAsync(configFile, JsonSerializer.Serialize(new ClientConfig() { Id = Guid.NewGuid(), DefaultUrl = DefaultRemote }));
            }

            var configJson = await File.ReadAllTextAsync(configFile);
            return JsonSerializer.Deserialize<ClientConfig>(configJson);
        }
    }
}
