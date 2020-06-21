using System;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Extensions;
using Libplanet.Tx;

namespace FreeMarketOne.BlockChain.Policy
{
    /// <summary>
    /// A default implementation of <see cref="IBlockPolicy{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">An <see cref="IAction"/> type.  It should match
    /// to <see cref="Block{T}"/>'s type parameter.</typeparam>
    public class BaseBlockPolicy<T> : IDefaultBlockPolicy<T>
        where T : IAction, new()
    {
        private readonly Predicate<Transaction<T>> _doesTransactionFollowPolicy;

        /// <summary>
        /// Creates a <see cref="BaseBlockPolicy{T}"/> with configuring
        /// <see cref="BlockInterval"/> in milliseconds,
        /// <see cref="MinimumDifficulty"/> and
        /// <see cref="DifficultyBoundDivisor"/>.
        /// </summary>
        /// <param name="blockAction">A block action to execute and be rendered for every block.
        /// </param>
        /// <param name="blockIntervalMilliseconds">Configures
        /// <see cref="BlockInterval"/> in milliseconds.
        /// 5000 milliseconds by default.
        /// </param>
        /// <param name="minimumDifficulty">Configures
        /// <see cref="MinimumDifficulty"/>. 1024 by default.</param>
        /// <param name="poolCheckIntervalMilliseconds">Configures
        /// <see cref="PoolCheckInterval"/> in milliseconds.
        /// 5000 milliseconds by default.
        /// </param>
        /// <param name="validBlockInterval">Configures validity
        /// <see cref="ValidBlockInterval"/> in milliseconds.
        /// null by default.
        /// </param>
        /// <param name="doesTransactionFollowPolicy">
        /// A predicate that determines if the transaction follows the block policy.
        /// </param>
        public BaseBlockPolicy(
            IAction blockAction = null,
            int blockIntervalMilliseconds = 5000,
            long minimumDifficulty = 1024,
            int poolCheckIntervalMilliseconds = 5000,
            int? validBlockInterval = null,
            Predicate<Transaction<T>> doesTransactionFollowPolicy = null)
            : this(
                blockAction,
                TimeSpan.FromMilliseconds(blockIntervalMilliseconds),
                minimumDifficulty,
                TimeSpan.FromMilliseconds(poolCheckIntervalMilliseconds),
                (validBlockInterval.HasValue ? TimeSpan.FromMilliseconds(validBlockInterval.Value) : default(TimeSpan?)),
                doesTransactionFollowPolicy)
        {
        }

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
        /// <param name="poolCheckInterval">Configures <see cref="PoolCheckInterval"/>.
        /// </param>        
        /// <param name="validBlockInterval">Configures <see cref="ValidBlockInterval"/> validity default is null (no expiration)</param>
        /// <param name="difficultyBoundDivisor">Configures
        /// <see cref="DifficultyBoundDivisor"/>.</param>
        /// <param name="doesTransactionFollowPolicy">
        /// A predicate that determines if the transaction follows the block policy.
        /// </param>
        public BaseBlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long minimumDifficulty,
            TimeSpan poolCheckInterval,
            TimeSpan? validBlockInterval,
            Predicate<Transaction<T>> doesTransactionFollowPolicy = null)
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
            PoolCheckInterval = poolCheckInterval;
            ValidBlockInterval = validBlockInterval;
            _doesTransactionFollowPolicy = doesTransactionFollowPolicy ?? (_ => true);
        }

        /// <inheritdoc/>
        public IAction BlockAction { get; }

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

        public bool DoesTransactionFollowsPolicy(Transaction<T> transaction)
        {
            return _doesTransactionFollowPolicy(transaction);
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
    }
}
