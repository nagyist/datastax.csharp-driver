//
//      Copyright (C) 2012 DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

using System;
using System.Threading;
using Cassandra.IntegrationTests.Core.Policies;

namespace Cassandra.IntegrationTests.Core
{
    [TestClass]
    public class RetryPolicyTests : PolicyTestTools
    {
        [TestMethod]
        [WorksForMe]
        public void defaultRetryPolicy()
        {
            Builder builder = Cluster.Builder();
            defaultPolicyTest(builder);
        }

        [TestMethod]
        [WorksForMe]
        public void defaultLoggingPolicy()
        {
            var builder = Cluster.Builder().WithRetryPolicy(new LoggingRetryPolicy(new DefaultRetryPolicy()));
            defaultPolicyTest(builder);
        }

        /*
         * Test the FallthroughRetryPolicy.
         * Uses the same code that DefaultRetryPolicy uses.
         */

        [TestMethod]
        [WorksForMe]
        public void fallthroughRetryPolicy()
        {
            Builder builder = Cluster.Builder().WithRetryPolicy(FallthroughRetryPolicy.Instance);
            defaultPolicyTest(builder);
        }

        /*
         * Test the FallthroughRetryPolicy with Logging enabled.
         * Uses the same code that DefaultRetryPolicy uses.
         */

        [TestMethod]
        [WorksForMe]
        public void fallthroughLoggingPolicy()
        {
            Builder builder = Cluster.Builder().WithRetryPolicy(new LoggingRetryPolicy(FallthroughRetryPolicy.Instance));
            defaultPolicyTest(builder);
        }

        public void defaultPolicyTest(Builder builder)
        {
            CCMBridge.CCMCluster c = CCMBridge.CCMCluster.Create(2, builder);
            createSchema(c.Session);

            // FIXME: Race condition where the nodes are not fully up yet and assertQueried reports slightly different numbers with fallthrough*Policy
            Thread.Sleep(5000);
            try
            {
                init(c, 12);
                query(c, 12);

                assertQueried(Options.Default.IP_PREFIX + "1", 6);
                assertQueried(Options.Default.IP_PREFIX + "2", 6);

                resetCoordinators();

                // Test reads
                bool successfulQuery = false;
                bool readTimeoutOnce = false;
                bool unavailableOnce = false;
                bool restartOnce = false;
                for (int i = 0; i < 100; ++i)
                {
                    try
                    {
                        // Force a ReadTimeoutException to be performed once
                        if (!readTimeoutOnce)
                        {
                            c.CCMBridge.ForceStop(2);
                        }

                        // Force an UnavailableException to be performed once
                        if (readTimeoutOnce && !unavailableOnce)
                        {
                            TestUtils.waitForDownWithWait(Options.Default.IP_PREFIX + "2", c.Cluster, 5);
                        }

                        // Bring back node to ensure other errors are not thrown on restart
                        if (unavailableOnce && !restartOnce)
                        {
                            c.CCMBridge.Start(2);
                            restartOnce = true;
                        }

                        query(c, 12);

                        if (restartOnce)
                            successfulQuery = true;
                    }
                    catch (UnavailableException)
                    {
                        //                        Assert.Equal("Not enough replica available for query at consistency ONE (1 required but only 0 alive)".ToLower(), e.Message.ToLower());
                        unavailableOnce = true;
                    }
                    catch (ReadTimeoutException)
                    {
                        //                        Assert.Equal("Cassandra timeout during read query at consistency ONE (1 responses were required but only 0 replica responded)".ToLower(), e.Message.ToLower());
                        readTimeoutOnce = true;
                    }
                }

                // Ensure the full cycle was completed
                Assert.True(successfulQuery, "Hit testing race condition. [Never completed successfully.] (Shouldn't be an issue.):\n");
                Assert.True(readTimeoutOnce, "Hit testing race condition. [Never encountered a ReadTimeoutException.] (Shouldn't be an issue.):\n");
                Assert.True(unavailableOnce, "Hit testing race condition. [Never encountered an UnavailableException.] (Shouldn't be an issue.):\n");

                // A weak test to ensure that the nodes were contacted
                assertQueriedAtLeast(Options.Default.IP_PREFIX + "1", 1);
                assertQueriedAtLeast(Options.Default.IP_PREFIX + "2", 1);

                resetCoordinators();


                // Test writes
                successfulQuery = false;
                bool writeTimeoutOnce = false;
                unavailableOnce = false;
                restartOnce = false;
                for (int i = 0; i < 100; ++i)
                {
                    try
                    {
                        // Force a WriteTimeoutException to be performed once
                        if (!writeTimeoutOnce)
                        {
                            c.CCMBridge.ForceStop(2);
                        }

                        // Force an UnavailableException to be performed once
                        if (writeTimeoutOnce && !unavailableOnce)
                        {
                            TestUtils.waitForDownWithWait(Options.Default.IP_PREFIX + "2", c.Cluster, 5);
                        }

                        // Bring back node to ensure other errors are not thrown on restart
                        if (unavailableOnce && !restartOnce)
                        {
                            c.CCMBridge.Start(2);
                            restartOnce = true;
                        }

                        init(c, 12);

                        if (restartOnce)
                            successfulQuery = true;
                    }
                    catch (UnavailableException)
                    {
                        //                        Assert.Equal("Not enough replica available for query at consistency ONE (1 required but only 0 alive)".ToLower(), e.Message.ToLower());
                        unavailableOnce = true;
                    }
                    catch (WriteTimeoutException)
                    {
                        //                        Assert.Equal("Cassandra timeout during write query at consistency ONE (1 replica were required but only 0 acknowledged the write)".ToLower(), e.Message.ToLower());
                        writeTimeoutOnce = true;
                    }
                }
                // Ensure the full cycle was completed
                Assert.True(successfulQuery, "Hit testing race condition. [Never completed successfully.] (Shouldn't be an issue.):\n");
                Assert.True(writeTimeoutOnce, "Hit testing race condition. [Never encountered a ReadTimeoutException.] (Shouldn't be an issue.):\n");
                Assert.True(unavailableOnce, "Hit testing race condition. [Never encountered an UnavailableException.] (Shouldn't be an issue.):\n");

                // TODO: Missing test to see if nodes were written to

                // Test batch writes
                successfulQuery = false;
                writeTimeoutOnce = false;
                unavailableOnce = false;
                restartOnce = false;
                for (int i = 0; i < 100; ++i)
                {
                    try
                    {
                        // Force a WriteTimeoutException to be performed once
                        if (!writeTimeoutOnce)
                        {
                            c.CCMBridge.ForceStop(2);
                        }

                        // Force an UnavailableException to be performed once
                        if (writeTimeoutOnce && !unavailableOnce)
                        {
                            TestUtils.waitForDownWithWait(Options.Default.IP_PREFIX + "2", c.Cluster, 5);
                        }

                        // Bring back node to ensure other errors are not thrown on restart
                        if (unavailableOnce && !restartOnce)
                        {
                            c.CCMBridge.Start(2);
                            restartOnce = true;
                        }

                        init(c, 12, true);

                        if (restartOnce)
                            successfulQuery = true;
                    }
                    catch (UnavailableException)
                    {
                        //                        Assert.Equal("Not enough replica available for query at consistency ONE (1 required but only 0 alive)", e.Message);
                        unavailableOnce = true;
                    }
                    catch (WriteTimeoutException)
                    {
                        //                        Assert.Equal("Cassandra timeout during write query at consistency ONE (1 replica were required but only 0 acknowledged the write)", e.Message);
                        writeTimeoutOnce = true;
                    }
                }
                // Ensure the full cycle was completed
                Assert.True(successfulQuery, "Hit testing race condition. [Never completed successfully.] (Shouldn't be an issue.):\n");
                Assert.True(writeTimeoutOnce, "Hit testing race condition. [Never encountered a ReadTimeoutException.] (Shouldn't be an issue.):\n");
                Assert.True(unavailableOnce, "Hit testing race condition. [Never encountered an UnavailableException.] (Shouldn't be an issue.):\n");

                // TODO: Missing test to see if nodes were written to
            }
            catch (Exception e)
            {
                c.ErrorOut();
                throw e;
            }
            finally
            {
                resetCoordinators();
                c.Discard();
            }
        }

        /// <summary>
        ///  Tests DowngradingConsistencyRetryPolicy
        /// </summary>
        [TestMethod]
        [WorksForMe]
        public void downgradingConsistencyRetryPolicy()
        {
            Builder builder = Cluster.Builder().WithRetryPolicy(DowngradingConsistencyRetryPolicy.Instance);
            downgradingConsistencyRetryPolicy(builder);
        }

        /// <summary>
        ///  Tests DowngradingConsistencyRetryPolicy with LoggingRetryPolicy
        /// </summary>
        [TestMethod]
        [WorksForMe]
        public void downgradingConsistencyLoggingPolicy()
        {
            Builder builder = Cluster.Builder().WithRetryPolicy(new LoggingRetryPolicy(DowngradingConsistencyRetryPolicy.Instance));
            downgradingConsistencyRetryPolicy(builder);
        }
        /// <summary>
        /// Unit test on retry decisions
        /// </summary>
        [TestMethod]
        public void DowngradingConsistencyRetryTest()
        {
            //Retry if 1 of 2 replicas are alive
            var decision = Session.GetRetryDecision(null, new UnavailableException(ConsistencyLevel.Two, 2, 1), DowngradingConsistencyRetryPolicy.Instance, 0);
            Assert.True(decision != null && decision.DecisionType == RetryDecision.RetryDecisionType.Retry);

            //Retry if 2 of 3 replicas are alive
            decision = Session.GetRetryDecision(null, new UnavailableException(ConsistencyLevel.Three, 3, 2), DowngradingConsistencyRetryPolicy.Instance, 0);
            Assert.True(decision != null && decision.DecisionType == RetryDecision.RetryDecisionType.Retry);

            //Throw if 0 replicas are alive
            decision = Session.GetRetryDecision(null, new UnavailableException(ConsistencyLevel.Three, 3, 0), DowngradingConsistencyRetryPolicy.Instance, 0);
            Assert.True(decision != null && decision.DecisionType == RetryDecision.RetryDecisionType.Rethrow);

            //Retry if 1 of 3 replicas is alive
            decision = Session.GetRetryDecision(null, new ReadTimeoutException(ConsistencyLevel.All, 3, 1, false), DowngradingConsistencyRetryPolicy.Instance, 0);
            Assert.True(decision != null && decision.DecisionType == RetryDecision.RetryDecisionType.Retry);
        }

        /// <summary>
        ///  Tests DowngradingConsistencyRetryPolicy
        /// </summary>
        public void downgradingConsistencyRetryPolicy(Builder builder)
        {
            CCMBridge.CCMCluster c = CCMBridge.CCMCluster.Create(3, builder);
            createSchema(c.Session, 3);

            // FIXME: Race condition where the nodes are not fully up yet and assertQueried reports slightly different numbers
            Thread.Sleep(5000);
            try
            {
                init(c, 12, ConsistencyLevel.All);

                query(c, 12, ConsistencyLevel.All);
                assertAchievedConsistencyLevel(ConsistencyLevel.All);

                //Kill one node: 2 nodes alive
                c.CCMBridge.ForceStop(2);
                TestUtils.waitForDownWithWait(Options.Default.IP_PREFIX + "2", c.Cluster, 20);

                //After killing one node, the achieved consistency level should be downgraded
                resetCoordinators();
                query(c, 12, ConsistencyLevel.All);
                assertAchievedConsistencyLevel(ConsistencyLevel.Two);

            }
            catch (Exception)
            {
                c.ErrorOut();
                throw;
            }
            finally
            {
                resetCoordinators();
                c.Discard();
            }
        }

        /*
         * Test the AlwaysIgnoreRetryPolicy with Logging enabled.
         */

        [TestMethod]
        [WorksForMe]
        public void alwaysIgnoreRetryPolicyTest()
        {
            Builder builder = Cluster.Builder().WithRetryPolicy(new LoggingRetryPolicy(AlwaysIgnoreRetryPolicy.Instance));
            CCMBridge.CCMCluster c = CCMBridge.CCMCluster.Create(2, builder);
            createSchema(c.Session);

            try
            {
                init(c, 12);
                query(c, 12);

                assertQueried(Options.Default.IP_PREFIX + "1", 6);
                assertQueried(Options.Default.IP_PREFIX + "2", 6);

                resetCoordinators();

                // Test failed reads
                c.CCMBridge.ForceStop(2);
                for (int i = 0; i < 10; ++i)
                {
                    query(c, 12);
                }

                // A weak test to ensure that the nodes were contacted
                assertQueried(Options.Default.IP_PREFIX + "1", 120);
                assertQueried(Options.Default.IP_PREFIX + "2", 0);
                resetCoordinators();


                c.CCMBridge.Start(2);
                TestUtils.waitFor(Options.Default.IP_PREFIX + "2", c.Cluster, 30);

                // Test successful reads
                for (int i = 0; i < 10; ++i)
                {
                    query(c, 12);
                }

                // A weak test to ensure that the nodes were contacted
                assertQueriedAtLeast(Options.Default.IP_PREFIX + "1", 1);
                assertQueriedAtLeast(Options.Default.IP_PREFIX + "2", 1);
                resetCoordinators();


                // Test writes
                for (int i = 0; i < 100; ++i)
                {
                    init(c, 12);
                }

                // TODO: Missing test to see if nodes were written to


                // Test failed writes
                c.CCMBridge.ForceStop(2);
                for (int i = 0; i < 100; ++i)
                {
                    init(c, 12);
                }

                // TODO: Missing test to see if nodes were written to
            }
            catch (Exception e)
            {
                c.ErrorOut();
                throw e;
            }
            finally
            {
                resetCoordinators();
                c.Discard();
            }
        }


        /*
         * Test the AlwaysIgnoreRetryPolicy with Logging enabled.
         */

        [TestMethod]
        [WorksForMe]
        public void alwaysRetryRetryPolicyTest()
        {
            Console.Write("MainThread is");
            Console.Write("[");
            Console.Write(Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("]");

            Builder builder = Cluster.Builder().WithRetryPolicy(new LoggingRetryPolicy(AlwaysRetryRetryPolicy.Instance));
            CCMBridge.CCMCluster c = CCMBridge.CCMCluster.Create(2, builder);
            createSchema(c.Session);

            try
            {
                init(c, 12);
                query(c, 12);

                assertQueried(Options.Default.IP_PREFIX + "1", 6);
                assertQueried(Options.Default.IP_PREFIX + "2", 6);

                resetCoordinators();

                // Test failed reads
                c.CCMBridge.ForceStop(2);

                var t1 = new Thread(() =>
                {
                    Console.Write("Thread started");
                    Console.Write("[");
                    Console.Write(Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine("]");

                    try
                    {
                        query(c, 12);
                    }
                    catch (ThreadInterruptedException)
                    {
                        Console.Write("Thread broke");
                        Console.Write("[");
                        Console.Write(Thread.CurrentThread.ManagedThreadId);
                        Console.WriteLine("]");
                    }
                    Console.Write("Thread finished");
                    Console.Write("[");
                    Console.Write(Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine("]");
                });
                t1.Start();
                t1.Join(10000);
                if (t1.IsAlive)
                    t1.Interrupt();

                t1.Join();

                // A weak test to ensure that the nodes were contacted
                assertQueried(Options.Default.IP_PREFIX + "1", 0);
                assertQueried(Options.Default.IP_PREFIX + "2", 0);
                resetCoordinators();


                c.CCMBridge.Start(2);
                TestUtils.waitFor(Options.Default.IP_PREFIX + "2", c.Cluster, 30);

                Console.Write("MainThread started");
                Console.Write("[");
                Console.Write(Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine("]");

                // Test successful reads
                for (int i = 0; i < 10; ++i)
                {
                    try
                    {
                        query(c, 12);
                    }
                    catch (ThreadInterruptedException)
                    {
                        Console.Write("Main Thread broke");
                        Console.Write("[");
                        Console.Write(Thread.CurrentThread.ManagedThreadId);
                        Console.WriteLine("]");
                    }
                }

                Console.Write("Main Thread finished");
                Console.Write("[");
                Console.Write(Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine("]");

                // A weak test to ensure that the nodes were contacted
                assertQueriedAtLeast(Options.Default.IP_PREFIX + "1", 1);
                assertQueriedAtLeast(Options.Default.IP_PREFIX + "2", 1);
                resetCoordinators();


                // Test writes
                for (int i = 0; i < 100; ++i)
                {
                    init(c, 12);
                }

                // TODO: Missing test to see if nodes were written to


                // Test failed writes
                c.CCMBridge.ForceStop(2);
                var t2 = new Thread(() =>
                {
                    Console.WriteLine("2 Thread started");
                    try
                    {
                        init(c, 12);
                        Assert.Fail();
                    }
                    catch (ThreadInterruptedException)
                    {
                        Console.WriteLine("2 Thread async call broke");
                    }
                    catch (NoHostAvailableException)
                    {
                        Console.WriteLine("2 Thread no host");
                    }
                    Console.WriteLine("2 Thread finished");
                });
                t2.Start();
                t2.Join(10000);
                if (t2.IsAlive)
                    t2.Interrupt();

                t2.Join();

                // TODO: Missing test to see if nodes were written to
            }
            catch (Exception e)
            {
                c.ErrorOut();
                throw e;
            }
            finally
            {
                resetCoordinators();
                c.Discard();
            }
        }

        public class TestRetryPolicy : IRetryPolicy
        {
            public RetryDecision OnReadTimeout(IStatement query, ConsistencyLevel cl, int requiredResponses, int receivedResponses, bool dataRetrieved,
                                               int nbRetry)
            {
                return RetryDecision.Rethrow();
            }

            public RetryDecision OnWriteTimeout(IStatement query, ConsistencyLevel cl, string writeType, int requiredAcks, int receivedAcks, int nbRetry)
            {
                return RetryDecision.Rethrow();
            }

            public RetryDecision OnUnavailable(IStatement query, ConsistencyLevel cl, int requiredReplica, int aliveReplica, int nbRetry)
            {
                return RetryDecision.Rethrow();
            }
        }
    }
}