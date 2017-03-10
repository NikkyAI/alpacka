using System;

namespace GitMC.Lib.Config
{
    [Flags]
    public enum Side
    {
        Client = 0b01,
        Server = 0b10,
        Both   = Client | Server
    }
    
    public static class SideExtensions
    {
        public static bool isClient(this Side side) =>
            ((side & Side.Client) == Side.Client);
        
        public static bool isServer(this Side side) =>
            ((side & Side.Server) == Side.Server);
    }
}
