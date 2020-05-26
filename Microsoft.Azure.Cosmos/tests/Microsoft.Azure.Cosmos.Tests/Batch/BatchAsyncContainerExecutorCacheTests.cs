﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]    
    public class BatchAsyncContainerExecutorCacheTests
    {
        [TestMethod]
        public async Task ConcurrentGet_ReturnsSameExecutorInstance()
        {
            CosmosClientContext context = this.MockClientContext();

            DatabaseInternal db = new DatabaseInlineCore(context, "test");

            List<Task<ContainerInternal>> tasks = new List<Task<ContainerInternal>>();
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(() => Task.FromResult((ContainerInternal)new ContainerInlineCore(context, db, "test"))));
            }

            await Task.WhenAll(tasks);

            BatchAsyncContainerExecutor firstExecutor = tasks[0].Result.BatchExecutor;
            Assert.IsNotNull(firstExecutor);
            for (int i = 1; i < 20; i++)
            {
                BatchAsyncContainerExecutor otherExecutor = tasks[i].Result.BatchExecutor;
                Assert.AreEqual(firstExecutor, otherExecutor);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public async Task SingleTaskScheduler_ExecutorTest()
        {
            CosmosClientContext context = this.MockClientContext();

            DatabaseInternal db = new DatabaseInlineCore(context, "test");

            List<Task<ContainerInternal>> tasks = new List<Task<ContainerInternal>>();
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(
                    Task.Factory.StartNew(() => (ContainerInternal)new ContainerInlineCore(context, db, "test"),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    new SingleTaskScheduler()));
            }

            await Task.WhenAll(tasks);

            BatchAsyncContainerExecutor firstExecutor = tasks[0].Result.BatchExecutor;
            Assert.IsNotNull(firstExecutor);
            for (int i = 1; i < 20; i++)
            {
                BatchAsyncContainerExecutor otherExecutor = tasks[i].Result.BatchExecutor;
                Assert.AreEqual(firstExecutor, otherExecutor);
            }
        }

        private CosmosClientContext MockClientContext(bool allowBulkExecution = true)
        {
            Mock<CosmosClient> mockClient = new Mock<CosmosClient>();
            mockClient.Setup(x => x.Endpoint).Returns(new Uri("http://localhost"));

            return ClientContextCore.Create(
                mockClient.Object,
                new MockDocumentClient(),
                new CosmosClientOptions() { AllowBulkExecution = allowBulkExecution });
        }
    }
}
