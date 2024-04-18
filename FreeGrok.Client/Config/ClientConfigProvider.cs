using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeGrok.Client.Config
{
    public class ClientConfigProvider
    {
        private const string DefaultRemote = "https://localhost:5001/";

        private readonly string configDirectory;
        private readonly string configFilePath;
        private ClientConfig clientConfig;

        public ClientConfigProvider()
        {
            this.configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FreeGrok");
            this.configFilePath = Path.Combine(configDirectory, "config.json");
        }



        public async Task InitializeAsync()
        {
            if (!File.Exists(configFilePath))
            {
                if (!Directory.Exists(configDirectory))
                {
                    Directory.CreateDirectory(configDirectory);
                }
                await File.WriteAllTextAsync(configFilePath, JsonSerializer.Serialize(new ClientConfig() { Id = Guid.NewGuid(), RemoteUrl = DefaultRemote }));
            }

            var configJson = await File.ReadAllTextAsync(configFilePath);
            clientConfig = JsonSerializer.Deserialize<ClientConfig>(configJson);
        }

        public async Task SetRemote(string remote)
        {
            clientConfig.RemoteUrl = remote;
            await File.WriteAllTextAsync(configFilePath, JsonSerializer.Serialize(clientConfig));
        }

        public ClientConfig ClientConfig => clientConfig;
    }
}
