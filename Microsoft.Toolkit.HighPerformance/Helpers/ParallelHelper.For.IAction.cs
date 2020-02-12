﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Toolkit.HighPerformance.Helpers
{
    /// <summary>
    /// Helpers to work with parallel code in a highly optimized manner.
    /// </summary>
    public static partial class ParallelHelper
    {
        /// <summary>
        /// Executes a specified action in an optimized parallel loop.
        /// </summary>
        /// <typeparam name="TAction">The type of action (implementing <see cref="IAction"/>) to invoke for each iteration index.</typeparam>
        /// <param name="start">The starting iteration index.</param>
        /// <param name="end">The final iteration index (exclusive).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void For<TAction>(int start, int end)
            where TAction : struct, IAction
        {
            For(start, end, default(TAction), 1);
        }

        /// <summary>
        /// Executes a specified action in an optimized parallel loop.
        /// </summary>
        /// <typeparam name="TAction">The type of action (implementing <see cref="IAction"/>) to invoke for each iteration index.</typeparam>
        /// <param name="start">The starting iteration index.</param>
        /// <param name="end">The final iteration index (exclusive).</param>
        /// <param name="minimumActionsPerThread">
        /// The minimum number of actions to run per individual thread. Set to 1 if all invocations
        /// should be parallelized, or to a greater number if each individual invocation is fast
        /// enough that it is more efficient to set a lower bound per each running thread.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void For<TAction>(int start, int end, int minimumActionsPerThread)
            where TAction : struct, IAction
        {
            For(start, end, default(TAction), minimumActionsPerThread);
        }

        /// <summary>
        /// Executes a specified action in an optimized parallel loop.
        /// </summary>
        /// <typeparam name="TAction">The type of action (implementing <see cref="IAction"/>) to invoke for each iteration index.</typeparam>
        /// <param name="start">The starting iteration index.</param>
        /// <param name="end">The final iteration index (exclusive).</param>
        /// <param name="action">The <typeparamref name="TAction"/> instance representing the action to invoke.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void For<TAction>(int start, int end, TAction action)
            where TAction : struct, IAction
        {
            For(start, end, action, 1);
        }

        /// <summary>
        /// Executes a specified action in an optimized parallel loop.
        /// </summary>
        /// <typeparam name="TAction">The type of action (implementing <see cref="IAction"/>) to invoke for each iteration index.</typeparam>
        /// <param name="start">The starting iteration index.</param>
        /// <param name="end">The final iteration index (exclusive).</param>
        /// <param name="action">The <typeparamref name="TAction"/> instance representing the action to invoke.</param>
        /// <param name="minimumActionsPerThread">
        /// The minimum number of actions to run per individual thread. Set to 1 if all invocations
        /// should be parallelized, or to a greater number if each individual invocation is fast
        /// enough that it is more efficient to set a lower bound per each running thread.
        /// </param>
        public static void For<TAction>(int start, int end, TAction action, int minimumActionsPerThread)
            where TAction : struct, IAction
        {
            if (minimumActionsPerThread <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumActionsPerThread),
                    "Each thread needs to perform at least one action");
            }

            if (start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start must be less than end");
            }

            if (start == end)
            {
                return;
            }

            int
                count = Math.Abs(start - end),
                cores = Environment.ProcessorCount,
                maxBatches = 1 + ((count - 1) / minimumActionsPerThread),
                numBatches = Math.Min(maxBatches, cores);

            // Skip the parallel invocation when a single batch is needed
            if (numBatches == 1)
            {
                for (int i = start; i < end; i++)
                {
                    action.Invoke(i);
                }

                return;
            }

            int batchSize = 1 + ((count - 1) / numBatches);

            var actionInvoker = new ActionInvoker<TAction>(start, end, action, batchSize);

            // Run the batched operations in parallel
            Parallel.For(
                0,
                numBatches,
                new ParallelOptions { MaxDegreeOfParallelism = numBatches },
                actionInvoker.Invoke);
        }

        // Wrapping struct acting as explicit closure to execute the processing batches
        private readonly struct ActionInvoker<TAction>
            where TAction : struct, IAction
        {
            private readonly int start;
            private readonly int end;
            private readonly TAction action;
            private readonly int batchSize;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ActionInvoker(int start, int end, TAction action, int batchSize)
            {
                this.start = start;
                this.end = end;
                this.action = action;
                this.batchSize = batchSize;
            }

            /// <summary>
            /// Processes the batch of actions at a specified index
            /// </summary>
            /// <param name="i">The index of the batch to process</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Invoke(int i)
            {
                int
                    offset = i * batchSize,
                    low = start + offset,
                    high = low + batchSize,
                    stop = Math.Min(high, end);

                for (int j = low; j < stop; j++)
                {
                    Unsafe.AsRef(action).Invoke(j);
                }
            }
        }
    }

    /// <summary>
    /// A contract for actions being executed with an input index.
    /// </summary>
    /// <remarks>If the <see cref="Invoke"/> method is small enough, it is highly recommended to mark it with <see cref="MethodImplOptions.AggressiveInlining"/>.</remarks>
    public interface IAction
    {
        /// <summary>
        /// Executes the action associated with a specific index.
        /// </summary>
        /// <param name="i">The current index for the action to execute.</param>
        void Invoke(int i);
    }
}