﻿using System.IO;
namespace ModHelper.PacketHandlers
{
    internal class ModNetHandler
    {
        public const byte RefreshingServer = 1;
        internal static RefreshServerPacketHandler RefreshServer = new RefreshServerPacketHandler(RefreshingServer);
        public static void HandlePacket(BinaryReader r, int fromWho)
        {
            switch (r.ReadByte())
            {
                case RefreshingServer:
                    RefreshServer.HandlePacket(r, fromWho);
                    break;
            }
        }
    }
}