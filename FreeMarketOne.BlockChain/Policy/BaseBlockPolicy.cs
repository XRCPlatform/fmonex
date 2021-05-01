using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using FreeMarketOne.DataStructure;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Extensions;
using Libplanet.Tx;

namespace FreeMarketOne.BlockChain.Policy
{
    [Serializable]
    public class InvalidBlockTypeActionException : InvalidBlockException
    {
        public InvalidBlockTypeActionException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// A default implementation of <see cref="IBlockPolicy{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">An <see cref="IAction"/> type.  It should match
    /// to <see cref="Block{T}"/>'s type parameter.</typeparam>
    public class BaseBlockPolicy<T> : IDefaultBlockPolicy<T>
        where T : IBaseAction, IAction, new()
    {
        private readonly int _maxBlockBytes;
        private readonly int _maxGenesisBytes;
        private readonly Func<Transaction<T>, BlockChain<T>, bool> _doesTransactionFollowPolicy;

        /// <summary>
        /// Creates a <see cref="BlockPolicy{T}"/> with configuring
        /// <see cref="BlockInterval"/>, <see cref="MinimumDifficulty"/> and
        /// <see cref="DifficultyBoundDivisor"/>.
        /// </summary>
        /// <param name="blockAction">A block action to execute and be rendered for every block.
        /// </param>
        /// <param name="blockInterval">Configures <see cref="BlockInterval"/>.
        /// </param>
        /// <param name="minimumDifficulty">Configures
        /// <see cref="MinimumDifficulty"/>.</param>
        /// <param name="maxTransactionsPerBlock">Configures <see cref="MaxTransactionsPerBlock"/>.
        /// </param>
        /// <param name="maxBlockBytes">Configures <see cref="GetMaxBlockBytes(long)"/> where
        /// the block is not a genesis.</param>
        /// <param name="maxGenesisBytes">Configures <see cref="GetMaxBlockBytes(long)"/> where
        /// the block is a genesis.</param>
        /// <param name="doesTransactionFollowPolicy">
        /// A predicate that determines if the transaction follows the block policy.
        /// </param>
        public BaseBlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long minimumDifficulty,
            int maxTransactionsPerBlock,
            int maxBlockBytes,
            int maxGenesisBytes,
            TimeSpan poolCheckInterval,
            TimeSpan? validBlockInterval,
            Type validTypeOfAction,
            Type[] validTypesOfActionItems,
            Func<Transaction<T>, BlockChain<T>, bool> doesTransactionFollowPolicy = null)
        {
            if (blockInterval < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blockInterval),
                    "Interval between blocks cannot be negative.");
            }

            if (minimumDifficulty < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumDifficulty),
                    "Minimum difficulty must be greater than 0.");
            }

            BlockAction = blockAction;
            BlockInterval = blockInterval;
            MinimumDifficulty = minimumDifficulty;
            MaxTransactionsPerBlock = maxTransactionsPerBlock;
            _maxBlockBytes = maxBlockBytes;
            _maxGenesisBytes = maxGenesisBytes;
            PoolCheckInterval = poolCheckInterval;
            ValidBlockInterval = validBlockInterval;
            ValidTypeOfAction = validTypeOfAction;
            ValidTypesOfActionItems = validTypesOfActionItems;
            _doesTransactionFollowPolicy = doesTransactionFollowPolicy ?? ((_, __) => true);
        }

        /// <inheritdoc/>
        public IAction BlockAction { get; }

        /// <inheritdoc cref="IBlockPolicy{T}.MaxTransactionsPerBlock"/>
        public int MaxTransactionsPerBlock { get; }

        /// <summary>
        /// An appropriate interval between consecutive <see cref="Block{T}"/>s.
        /// It is usually from 20 to 30 seconds.
        /// <para>If a previous interval took longer than this
        /// <see cref="GetNextBlockDifficulty(BlockChain{T})"/> method
        /// raises the <see cref="Block{T}.Difficulty"/>.  If it took shorter
        /// than this <see cref="Block{T}.Difficulty"/> is dropped.</para>
        /// </summary>
        public TimeSpan BlockInterval { get; }

        /// <inheritdoc/>
        public TimeSpan PoolCheckInterval { get; }

        /// <inheritdoc/>
        public TimeSpan? ValidBlockInterval { get; }

        private long MinimumDifficulty { get; }

        public Type ValidTypeOfAction { get; }

        public Type[] ValidTypesOfActionItems { get; }

        public IComparer<BlockPerception> CanonicalChainComparer { get; } = new TotalDifficultyComparer(TimeSpan.FromSeconds(30));

        public bool DoesTransactionFollowsPolicy(Transaction<T> transaction,
            BlockChain<T> blockChain)
        {
            foreach (var itemAction in transaction.Actions)
            {
                foreach (var itemBase in itemAction.BaseItems)
                {
                    //Verify type of item in tx
                    if (!ValidTypesOfActionItems.Contains(itemBase.GetType()))
                    {
                        return false;
                    }

                    //Verify item internal integrity
                    if (!itemBase.IsValid())
                    {
                        return false;
                    }
                }
            }

            return _doesTransactionFollowPolicy(transaction, blockChain);
        }

        /// <inheritdoc/>
        public InvalidBlockException ValidateNextBlock(
            BlockChain<T> blocks,
            Block<T> nextBlock)
        {
            long index = blocks.Count;
            long difficulty = GetNextBlockDifficulty(blocks);

            Block<T> lastBlock = index >= 1 ? blocks[index - 1] : null;
            HashDigest<SHA256>? prevHash = lastBlock?.Hash;
            DateTimeOffset? prevTimestamp = lastBlock?.Timestamp;

            if (nextBlock.Index != index)
            {
                return new InvalidBlockIndexException(
                    $"the expected block index is {index}, but its index" +
                    $" is {nextBlock.Index}'");
            }

            if (nextBlock.Difficulty < difficulty)
            {
                return new InvalidBlockDifficultyException(
                    $"the expected difficulty of the block #{index} " +
                    $"is {difficulty}, but its difficulty is " +
                    $"{nextBlock.Difficulty}'");
            }

            if (!nextBlock.PreviousHash.Equals(prevHash))
            {
                if (prevHash is null)
                {
                    return new InvalidBlockPreviousHashException(
                        "the genesis block must have not previous block");
                }

                return new InvalidBlockPreviousHashException(
                    $"the block #{index} is not continuous from the " +
                    $"block #{index - 1}; while previous block's hash is " +
                    $"{prevHash}, the block #{index}'s pointer to " +
                    "the previous hash refers to " +
                    (nextBlock.PreviousHash?.ToString() ?? "nothing"));
            }

            if (nextBlock.Timestamp < prevTimestamp)
            {
                return new InvalidBlockTimestampException(
                    $"the block #{index}'s timestamp " +
                    $"({nextBlock.Timestamp}) is earlier than" +
                    $" the block #{index - 1}'s ({prevTimestamp})");
            }

            if (prevTimestamp.HasValue && prevTimestamp.Value.Add(BlockInterval) > nextBlock.Timestamp)
            {
                return new InvalidBlockTimestampException(
                    $"the block #{index}'s timestamp " +
                    $"({nextBlock.Timestamp}) is earlier than" +
                    $" the block #{index - 1}'s with block interval ({prevTimestamp.Value.Add(BlockInterval)})");
            }

            if (nextBlock.Transactions.Any())
            {
                foreach (var itemTx in nextBlock.Transactions)
                {
                    if (itemTx.Actions.Any()) { 
                        foreach (var itemAction in itemTx.Actions)
                        {
                            if (ValidTypeOfAction != itemAction.GetType())
                            {
                                return new InvalidBlockTypeActionException(
                                    $"Block contains wrong type of action ({itemAction.GetType()}) expecting ({ValidTypeOfAction})");
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <inheritdoc />
        public long GetNextBlockDifficulty(BlockChain<T> blocks)
        {
            long index = blocks.Count;

            if (index < 0)
            {
                throw new InvalidBlockIndexException(
                    $"index must be 0 or more, but its index is {index}.");
            }

            if (index <= 1)
            {
                return index == 0 ? 0 : MinimumDifficulty;
            } 
            else
            {
                return MinimumDifficulty;
            }
        }

        public TimeSpan GetApproxTimeSpanToMineNextBlock()
        {
            return BlockInterval + PoolCheckInterval;
        }

        public int GetMaxBlockBytes(long index) => index > 0 ? _maxBlockBytes : _maxGenesisBytes;
    }
}
