using Libplanet.Blocks;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE - extracted enum to it's own class as deleted the Message class which had this enum before
    public enum MessageType : byte
    {
        Unrecognized = 0x00,

        /// <summary>
        /// Check message to determine peer is alive.
        /// </summary>
        Ping = 0x01,

        /// <summary>
        /// A reply to <see cref="Ping"/>.
        /// </summary>
        Pong = 0x14,

        /// <summary>
        /// Request to query block hashes.
        /// </summary>
        GetBlockHashes = 0x04,

        /// <summary>
        /// Inventory to transfer transactions.
        /// </summary>
        TxIds = 0x06,

        /// <summary>
        /// Request to query blocks.
        /// </summary>
        GetBlocks = 0x07,

        /// <summary>
        /// Request to query transactions.
        /// </summary>
        GetTxs = 0x08,

        /// <summary>
        /// Message containing serialized blocks.
        /// </summary>
        Blocks = 0x0a,

        /// <summary>
        /// Message containing serialized transaction.
        /// </summary>
        Transactions = 0x10,

        /// <summary>
        /// Message containing request for nearby peers.
        /// </summary>
        FindNeighbors = 0x11,

        /// <summary>
        /// Message containing nearby peers.
        /// </summary>
        Neighbors = 0x12,

        /// <summary>
        /// Request to query calculated states.
        /// </summary>
        GetRecentStates = 0x0b,

        /// <summary>
        /// A reply to <see cref="GetRecentStates"/>.
        /// Contains the calculated recent states and state references.
        /// </summary>
        RecentStates = 0x13,

        /// <summary>
        /// Message containing a single <see cref="BlockHeader"/>.
        /// </summary>
        BlockHeaderMessage = 0x0c,

        /// <summary>
        /// Message containing demand block hashes with their index numbers.
        /// </summary>
        BlockHashes = 0x0e,

        /// <summary>
        /// Request current chain status of the peer.
        /// </summary>
        GetChainStatus = 0x20,

        /// <summary>
        /// A reply to <see cref="GetChainStatus"/>.
        /// Contains the chain status of the peer at the moment.
        /// </summary>
        ChainStatus = 0x24,

        /// <summary>
        /// Request a block's delta states.
        /// </summary>
        GetBlockStates = 0x22,

        /// <summary>
        /// A reply to <see cref="GetBlockStates"/>.
        /// Contains the delta states of the requested block.
        /// </summary>
        BlockStates = 0x23,

        /// <summary>
        /// A reply to any messages with different <see cref="AppProtocolVersion"/>.
        /// Contains the expected and actual <see cref="AppProtocolVersion"/>
        /// value of the message.
        /// </summary>
        DifferentVersion = 0x30,

        //Used to announce new blocks to network, similar 
        //but must not clash with Blocks message as 
        //Blocks is a response to request and clash on routing.
        BlockBroadcast = 0x31,

        //Used to announce new staged transaction to nework similar to Tx
        //but must not clash with Tx as message Tx is a response object and break routing. 
        TxBroadcast = 0x32,
    }
}
