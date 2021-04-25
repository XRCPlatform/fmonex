using Bencodex;
using Bencodex.Types;
using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  changed to be ben coded message 
    public class RecentStates : IBenEncodeable
    {
        private static readonly Codec _codec = new Codec();

        private static readonly byte[] OffsetKey = { 0x4f }; // 'O'
        private static readonly byte[] IterationKey = { 0x49 }; // 'I'        
        private static readonly byte[] BlockHashKey = { 0x42 }; // 'B'
        private static readonly byte[] StateReferencesKey = { 0x53 }; // 'S'
        private static readonly byte[] BlockStatesKey = { 0x58 }; // 'X'
        private static readonly byte[] AccountsCountKey = { 0x41 }; // 'A'

        public RecentStates(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {
        }

        public RecentStates(Bencodex.Types.Dictionary dict)
        {
            if (dict.ContainsKey((IKey)(Binary)OffsetKey))
            {
                Offset = dict.GetValue<Integer>(OffsetKey);
            }

            if (dict.ContainsKey((IKey)(Binary)BlockHashKey))
            {
                BlockHash = new HashDigest<SHA256>(dict.GetValue<Binary>(BlockHashKey));
            }

            if (dict.ContainsKey((IKey)(Binary)BlockHashKey))
            {
                Iteration = dict.GetValue<Integer>(IterationKey);
            }


            if (dict.ContainsKey((IKey)(Binary)AccountsCountKey))
            {
                int accountsCount = dict.GetValue<Integer>(AccountsCountKey);
                if (accountsCount < 0)
                {
                    BlockStates = null;
                    StateReferences = null;
                    return;
                }
            }

            if (dict.ContainsKey((IKey)(Binary)StateReferencesKey))
            {
                var targetStateRefs = new System.Collections.Generic.Dictionary<string, IImmutableList<HashDigest<SHA256>>>();

                foreach (var item in dict.GetValue<Bencodex.Types.Dictionary>(StateReferencesKey))
                {
                    var key = Encoding.UTF8.GetString(item.Key.EncodeAsByteArray());
                    System.Collections.Generic.List<HashDigest<SHA256>> refs = new System.Collections.Generic.List<HashDigest<SHA256>>();
                    foreach (var h in (Bencodex.Types.List)item.Value)
                    {
                        refs.Add(new HashDigest<SHA256>((Binary)h));
                    }
                    targetStateRefs.Add(key, refs.ToImmutableList());
                }
                StateReferences = targetStateRefs.ToImmutableDictionary();
            }

            if (dict.ContainsKey((IKey)(Binary)BlockStatesKey))
            {
                var blockStates = new System.Collections.Generic.Dictionary<HashDigest<SHA256>, IImmutableDictionary<string, IValue>>();
                var blockState = dict.GetValue<Bencodex.Types.Dictionary>(BlockStatesKey);
                foreach (var item in blockState)
                {
                    var blockHash = new HashDigest<SHA256>(item.Key.EncodeAsByteArray());
                    var states = new System.Collections.Generic.Dictionary<string, IValue>();
                    //System.Collections.Generic.List<HashDigest<SHA256>> refs = new System.Collections.Generic.List<HashDigest<SHA256>>();
                    foreach (var h in (Bencodex.Types.Dictionary)item.Value)
                    {
                        var key = Encoding.UTF8.GetString(h.Key.EncodeAsByteArray());      
                        states.Add(key, Decompress((Binary)h.Value));
                    }
                    blockStates.Add(blockHash, states.ToImmutableDictionary());
                }
                BlockStates = blockStates.ToImmutableDictionary();
            }
        }


        public static RecentStates Deserialize(byte[] bytes)
        {
            return new RecentStates(DecodeBytesToBen(bytes));
        }

        public object FromBenBytes(byte[] bytes)
        {
            return Deserialize(bytes);
        }


        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }


        public RecentStates(HashDigest<SHA256> blockHash, long offset, int iteration,
            IImmutableDictionary<HashDigest<SHA256>, IImmutableDictionary<string, IValue>> blockStates,
            IImmutableDictionary<string, IImmutableList<HashDigest<SHA256>>> stateReferences
        )
        {
            BlockHash = blockHash;
            Offset = offset;
            Iteration = iteration;

            if (blockStates is null && stateReferences is null)
            {
                BlockStates = null;
                StateReferences = null;
                return;
            }

            if (blockStates is null)
            {
                throw new ArgumentNullException(nameof(blockStates));
            }
            else if (stateReferences is null)
            {
                throw new ArgumentNullException(nameof(stateReferences));
            }

            BlockStates = blockStates;
            StateReferences = stateReferences;
        }

        public HashDigest<SHA256> BlockHash { get; }

        public long Offset { get; }

        public int Iteration { get; }

        public bool Missing => BlockStates is null;

        public IImmutableDictionary<HashDigest<SHA256>, IImmutableDictionary<string, IValue>> BlockStates
        {
            get;
        }

        /// <summary>
        /// State references of all available accounts.  Each value has a list of
        /// state references in ascending order; the closest to the genesis block
        /// goes first, and the closest to the tip goes last.
        /// </summary>
        public IImmutableDictionary<string, IImmutableList<HashDigest<SHA256>>> StateReferences
        {
            get;
        }


        //public RecentStates(NetMQFrame[] frames)
        //{
        //    IEnumerator<NetMQFrame> it = ((IEnumerable<NetMQFrame>)frames).GetEnumerator();

        //    it.MoveNext();
        //    BlockHash = new HashDigest<SHA256>(it.Current.Buffer);

        //    it.MoveNext();
        //    Offset = it.Current.ConvertToInt64();

        //    it.MoveNext();
        //    Iteration = it.Current.ConvertToInt32();

        //    it.MoveNext();
        //    int accountsCount = it.Current.ConvertToInt32();

        //    if (accountsCount < 0)
        //    {
        //        BlockStates = null;
        //        StateReferences = null;
        //        return;
        //    }

        //    var stateRefs =
        //        new Dictionary<string, IImmutableList<HashDigest<SHA256>>>(accountsCount);

        //    for (int j = 0; j < accountsCount; j++)
        //    {
        //        it.MoveNext();
        //        var key = Encoding.UTF8.GetString(it.Current.Buffer);

        //        it.MoveNext();
        //        int stateRefsLength = it.Current.ConvertToInt32();
        //        List<HashDigest<SHA256>> refs = new List<HashDigest<SHA256>>(stateRefsLength);

        //        for (int k = 0; k < stateRefsLength; k++)
        //        {
        //            it.MoveNext();
        //            refs.Add(new HashDigest<SHA256>(it.Current.Buffer));
        //        }

        //        stateRefs[key] = refs.ToImmutableList();
        //    }

        //    it.MoveNext();
        //    int blocksLength = it.Current.ConvertToInt32();  // This is not height!

        //    var blockStates =
        //        new Dictionary<HashDigest<SHA256>, IImmutableDictionary<string, IValue>>(
        //            blocksLength);

        //    for (int j = 0; j < blocksLength; j++)
        //    {
        //        it.MoveNext();
        //        var blockHash = new HashDigest<SHA256>(it.Current.Buffer);

        //        it.MoveNext();
        //        int statesLength = it.Current.ConvertToInt32();

        //        var states = new Dictionary<string, IValue>(statesLength);
        //        for (int k = 0; k < statesLength; k++)
        //        {
        //            it.MoveNext();
        //            var key = Encoding.UTF8.GetString(it.Current.Buffer);

        //            it.MoveNext();
        //            states[key] = Decompress(it.Current.Buffer);
        //        }

        //        blockStates[blockHash] = states.ToImmutableDictionary();
        //    }

        //    BlockStates = blockStates.ToImmutableDictionary();
        //    StateReferences = stateRefs.ToImmutableDictionary();
        //}
        public Dictionary ToBencodex()
        {
            /*
            Note that the data frames this property returns omit the very first three frames
            of (message type, public key, signature).  See also Message.ToNetMQMessage() method.

            | 1. BlockHash (32 bytes; SHA-256 digest)
            |   The requested block hash which corresponds to
            |   the given GetRecentStates.BlockHash value.
            +
            | 2. Offset (64 bytes; long)
            |   Indicates the latest block's offset that sender have
            |   sent state references and block states.
            +
            | 3. Iteration (32 bytes; int)
            |   Count of iterations to send full state references
            |   and block states.
            +
            | 4. StateReferences.Count (4 bytes; 32-bit integer in big endian)
            |   The number of the accounts of the following state references (4) in the payload.
            |   When Missing = true, this contains -1 and no data frames follow at all.
            +
            | 5. StateReferences [unordered]
            | | 5.1. Key (varying bytes; state key)
            | |   The state key of the following state references (5.3).
            | +
            | | 5.2. Value.Count (4 bytes; 32-bit integer in big endian)
            | |   The length of the following state references (5.3).
            | +
            | | 5.3. Value [descending order; the recent block goes first & the oldest goes last]
            | | | 5.3.1. (32 bytes; SHA-256 digest)
            | | |   A state reference of the account (5.1).
            +
            | 6. BlockStates.Count (4 bytes; 32-bit integer in big endian)
            |   The number of the following block states (7) in the payload.
            +
            | 7. BlockStates [unordered]
            | | 7.1. Key (32 bytes; SHA-256 digest)
            | |   A block hash having the following states delta (7.3).
            | +
            | | 7.2. Value.Count (4 bytes; 32-bit integer in big endian)
            | |   The number of accounts whose states changed in the following delta (7.3).
            | +
            | | 7.3. Value [unordered]
            | | | 7.3.1. Key (20 bytes; account address)
            | | |   An account address having the following updated state (7.3.2).
            | | +
            | | | 7.3.2. Value (varying bytes; <a href="https://bencodex.org">Bencodex</a> format)
            | | |   An updated state of the account (7.3.1).
            */

            var dict = Dictionary.Empty
                .Add(BlockHashKey, BlockHash.ToByteArray())
                .Add(OffsetKey, Offset)
                .Add(IterationKey, Iteration);

            if (Missing)
            {
                dict = dict.Add(AccountsCountKey, -1); ;
            }
            else
            {
                dict = dict.Add(AccountsCountKey, StateReferences.Count());
            }

            if (StateReferences.Any())
            {
                var dictStateRefs = new Bencodex.Types.Dictionary();
                foreach (var pair in StateReferences)
                {
                    var stateHashes = new System.Collections.Generic.List<IValue>();
                    foreach (var blockHash in pair.Value)
                    {
                        stateHashes.Add((Binary)blockHash.ToByteArray());
                    }

                    dictStateRefs = dictStateRefs.Add(Encoding.UTF8.GetBytes(pair.Key), stateHashes.ToImmutableList());
                }

                dict = dict.Add(StateReferencesKey, dictStateRefs);
            }

            if (BlockStates.Any())
            {
                var dictStateRefs = new Bencodex.Types.Dictionary();
                foreach (var blockState in BlockStates)
                {
                    var keyStatesDic = new Bencodex.Types.Dictionary();
                    foreach (var keyStates in blockState.Value)
                    {
                        keyStatesDic = keyStatesDic.Add(Encoding.UTF8.GetBytes(keyStates.Key), Compress(keyStates.Value));
                    }

                    dictStateRefs = dictStateRefs.Add(blockState.Key.ToByteArray(), keyStatesDic);
                }

                dict = dict.Add(BlockStatesKey, dictStateRefs);
            }

            return dict;
        }


        //protected override IEnumerable<NetMQFrame> DataFrames
        //{
        //    /*
        //    Note that the data frames this property returns omit the very first three frames
        //    of (message type, public key, signature).  See also Message.ToNetMQMessage() method.

        //    | 1. BlockHash (32 bytes; SHA-256 digest)
        //    |   The requested block hash which corresponds to
        //    |   the given GetRecentStates.BlockHash value.
        //    +
        //    | 2. Offset (64 bytes; long)
        //    |   Indicates the latest block's offset that sender have
        //    |   sent state references and block states.
        //    +
        //    | 3. Iteration (32 bytes; int)
        //    |   Count of iterations to send full state references
        //    |   and block states.
        //    +
        //    | 4. StateReferences.Count (4 bytes; 32-bit integer in big endian)
        //    |   The number of the accounts of the following state references (4) in the payload.
        //    |   When Missing = true, this contains -1 and no data frames follow at all.
        //    +
        //    | 5. StateReferences [unordered]
        //    | | 5.1. Key (varying bytes; state key)
        //    | |   The state key of the following state references (5.3).
        //    | +
        //    | | 5.2. Value.Count (4 bytes; 32-bit integer in big endian)
        //    | |   The length of the following state references (5.3).
        //    | +
        //    | | 5.3. Value [descending order; the recent block goes first & the oldest goes last]
        //    | | | 5.3.1. (32 bytes; SHA-256 digest)
        //    | | |   A state reference of the account (5.1).
        //    +
        //    | 6. BlockStates.Count (4 bytes; 32-bit integer in big endian)
        //    |   The number of the following block states (7) in the payload.
        //    +
        //    | 7. BlockStates [unordered]
        //    | | 7.1. Key (32 bytes; SHA-256 digest)
        //    | |   A block hash having the following states delta (7.3).
        //    | +
        //    | | 7.2. Value.Count (4 bytes; 32-bit integer in big endian)
        //    | |   The number of accounts whose states changed in the following delta (7.3).
        //    | +
        //    | | 7.3. Value [unordered]
        //    | | | 7.3.1. Key (20 bytes; account address)
        //    | | |   An account address having the following updated state (7.3.2).
        //    | | +
        //    | | | 7.3.2. Value (varying bytes; <a href="https://bencodex.org">Bencodex</a> format)
        //    | | |   An updated state of the account (7.3.1).
        //    */
        //    get
        //    {
        //        yield return new NetMQFrame(BlockHash.ToByteArray());
        //        yield return new NetMQFrame(NetworkOrderBitsConverter.GetBytes(Offset));
        //        yield return new NetMQFrame(NetworkOrderBitsConverter.GetBytes(Iteration));
        //        if (Missing)
        //        {
        //            yield return new NetMQFrame(NetworkOrderBitsConverter.GetBytes(-1));
        //            yield break;
        //        }

        //        yield return new NetMQFrame(
        //            NetworkOrderBitsConverter.GetBytes(StateReferences.Count)
        //        );

        //        foreach (var pair in StateReferences)
        //        {
        //            yield return new NetMQFrame(Encoding.UTF8.GetBytes(pair.Key));

        //            IImmutableList<HashDigest<SHA256>> stateRefs = pair.Value;
        //            yield return new NetMQFrame(
        //                NetworkOrderBitsConverter.GetBytes(stateRefs.Count)
        //            );

        //            foreach (HashDigest<SHA256> blockHash in stateRefs)
        //            {
        //                yield return new NetMQFrame(blockHash.ToByteArray());
        //            }
        //        }

        //        yield return new NetMQFrame(NetworkOrderBitsConverter.GetBytes(BlockStates.Count));
        //        var codec = new Codec();
        //        foreach (var blockState in BlockStates)
        //        {
        //            yield return new NetMQFrame(blockState.Key.ToByteArray());

        //            IImmutableDictionary<string, IValue> states = blockState.Value;
        //            yield return new NetMQFrame(NetworkOrderBitsConverter.GetBytes(states.Count));

        //            foreach (var keyStates in states)
        //            {
        //                yield return new NetMQFrame(Encoding.UTF8.GetBytes(keyStates.Key));
        //                yield return new NetMQFrame(Compress(keyStates.Value));
        //            }
        //        }
        //    }
        //}
        //}

        private byte[] Compress(IValue value)
        {
            using var buffer = new MemoryStream();
            using (var deflate = new DeflateStream(buffer, CompressionLevel.Fastest))
            {
                _codec.Encode(value, deflate);
            }

            return buffer.ToArray();
        }

        private IValue Decompress(byte[] bytes)
        {
            using var buffer = new MemoryStream();
            using var compressed = new MemoryStream(bytes);
            using (var deflate = new DeflateStream(compressed, CompressionMode.Decompress))
            {
                deflate.CopyTo(buffer);
            }

            buffer.Seek(0, SeekOrigin.Begin);
            return _codec.Decode(buffer);
        }

        private static Bencodex.Types.Dictionary DecodeBytesToBen(byte[] bytes)
        {
            IValue value = new Codec().Decode(bytes);
            if (!(value is Dictionary dict))
            {
                throw new DecodingException(
                    $"Expected {typeof(Dictionary)} but " +
                    $"{value.GetType()}");
            }
            return dict;
        }
    }
}
