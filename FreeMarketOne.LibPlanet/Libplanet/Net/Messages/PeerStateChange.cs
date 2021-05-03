namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  added new enum for peer state changes
    public enum PeerStateChange
    {
        Joined,
        Left,
        TwoWayDialogConfirmed,
        Removed,
        Banned
    }
}