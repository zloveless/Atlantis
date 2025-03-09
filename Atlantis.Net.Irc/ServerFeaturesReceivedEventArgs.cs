namespace Atlantis.Net.Irc;

public class ServerFeaturesReceivedEventArgs : EventArgs 
{
    public ServerFeaturesReceivedEventArgs(IDictionary<string, string> serverFeatures)
    {
        ServerFeatures = serverFeatures;
    }
    
    public IDictionary<string, string> ServerFeatures { get; }
}