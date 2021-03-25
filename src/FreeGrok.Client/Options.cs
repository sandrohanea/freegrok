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

        [Option('t', "Type", Required = false, Default = "https", HelpText = "Set the type of the forwarding.")]
        public string Type { get; set; }

        [Option('h', "host", Required = false, HelpText = "Set a value which will override the host header in all requests")]
        public string Host { get; set; }
    }
}
