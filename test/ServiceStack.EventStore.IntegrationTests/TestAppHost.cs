﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.EventStore.IntegrationTests
{
    using ConnectionManagement;

    using FluentAssertions;

    using Funq;

    using Logging;

    using Main;

    using Projections;

    using Subscriptions;

    public class TestAppHost : AppHostHttpListenerBase
    {
        /// <summary>
        /// Default constructor.
        /// Base constructor requires a name and assembly to locate web service classes. 
        /// </summary>
        public TestAppHost() : base("EventStoreListener", typeof(TestAppHost).Assembly) { }

        public override void Configure(Container container)
        {
            var settings = new SubscriptionSettings()
                .SubscribeToStreams(streams =>
                {
                    streams.Add(new ReadModelSubscription()
                        .SetRetryPolicy(new[] {1.Seconds(), 3.Seconds()})
                        .WithStorage(new ReadModelStorage(StorageType.Redis, "localhost:6379")));
                });

            var connection = new EventStoreConnectionSettings()
                                    .UserName("admin")
                                    .Password("changeit")
                                    .TcpEndpoint("localhost:1113")
                                    .HttpEndpoint("localhost:2113");

            LogManager.LogFactory = new ConsoleLogFactory();

            Plugins.Add(new MetadataFeature());
            Plugins.Add(new EventStoreFeature(connection, settings, typeof(TestAppHost).Assembly));
        }
    }


}
