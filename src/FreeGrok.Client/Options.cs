using CommandLine;

namespace FreeGrok.Client
{
    public class Options
    {
        [Option('p', "port", Required = true, HelpText = "Set the port which will be used with the localhost.")]
        public int Port { get; set; }


        [Option('d', "domain", Required = true, HelpText = "Set the public domain which will be used.")]
        public string Domain { get; set; }

        [Option('r', "remoteUrl", Required = false, HelpText = "Set the remote URL.")]
        public string Remote { get; set; }

        [Option('s', "useHttps", Required = false, Default = true, HelpText = "Set a flag indicating if Https should be used for localhost")]
        public bool UseHttps { get; set; }

        [Option('h', "host", Required = false, HelpText = "Set a value which will override the host header in all requests")]
        public string Host { get; set; }
    }
}
