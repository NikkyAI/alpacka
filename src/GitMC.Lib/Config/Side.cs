using System;

namespace GitMC.Lib.Config
{
    [Flags]
    public enum Side
    {
        None   = 0,
        Client = 0b01,
        Server = 0b10,
        Both   = Client | Server
    }
    
    public static class SideExtensions
    {
        public static bool IsClient(this Side side) =>
            ((side & Side.Client) == Side.Client);
        
        public static bool IsServer(this Side side) =>
            ((side & Side.Server) == Side.Server);
    }
}
