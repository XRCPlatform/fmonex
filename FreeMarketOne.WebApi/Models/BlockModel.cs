using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Tx;

namespace FreeMarketOne.WebApi.Models
{
    public class BlockModel<T> where T : IAction, new()
    {
        public string Hash { get; set; }

        public HashDigest<SHA256> PreEvaluationHash { get; set; }

        public HashDigest<SHA256>? StateRootHash { get; set; }

        public long Index { get; set; }

        public long Difficulty { get; set; }

        public BigInteger TotalDifficulty { get; set; }

        public Nonce Nonce { get; set; }

        public Address? Miner { get; set; }

        public HashDigest<SHA256>? PreviousHash { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public HashDigest<SHA256>? TxHash { get; set; }

        public IEnumerable<Transaction<T>> Transactions { get; set; }

        public static BlockModel<T> FromBlock(Block<T> block)
        {
            return new()
            {
                Hash = ByteUtil.Hex(block.Hash.ToByteArray()),
                PreEvaluationHash = block.PreEvaluationHash,
                StateRootHash = block.StateRootHash,
                Index = block.Index,
                Difficulty = block.Difficulty,
                TotalDifficulty = block.TotalDifficulty,
                Nonce = block.Nonce,
                Miner = block.Miner,
                PreviousHash = block.PreviousHash,
                Timestamp = block.Timestamp,
                TxHash = block.TxHash
            };
        }
    }
}