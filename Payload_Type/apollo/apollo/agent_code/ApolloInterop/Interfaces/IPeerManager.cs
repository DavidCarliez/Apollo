using AgInterop.Classes.P2P;
using AgInterop.Structs.MythicStructs;
namespace AgInterop.Interfaces
{
    public interface IPeerManager
    {
        Peer AddPeer(PeerInformation info);
        bool Remove(string uuid);
        bool Remove(IPeer peer);
        bool Route(DelegateMessage msg);
    }
}
