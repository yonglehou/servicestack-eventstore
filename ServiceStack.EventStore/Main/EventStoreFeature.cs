﻿namespace ServiceStack.EventStore.Main
{
    using System;
    using System.Collections.Generic;
    using global::EventStore.ClientAPI;
    using Funq;
    using ConnectionManagement;
    using Consumers;
    using Dispatcher;
    using Repository;
    using Subscriptions;
    using Logging;
    using Redis;
    using Events;
    using Projections;

    public class EventStoreFeature: IPlugin
    {
        private readonly EventStoreFeatureSettings featureSettings;
        private Container container;
        private readonly ILog log;
        private readonly EventStoreConnectionSettings connectionSettings;

        private readonly Dictionary<string, Type> consumers = new Dictionary<string, Type>
        {
            {"PersistentSubscription", typeof(PersistentConsumer)},
            {"CatchUpSubscription", typeof(CatchUpConsumer)},
            {"VolatileSubscription", typeof(VolatileConsumer)},
            {"ReadModelSubscription", typeof(ReadModelConsumer)}
        };

        public EventStoreFeature(EventStoreFeatureSettings featureSettings, EventStoreConnectionSettings connectionSettings)
        {
            this.featureSettings = featureSettings;
            this.connectionSettings = connectionSettings;
            EventTypes.ScanForAggregateEvents();
            EventTypes.ScanForServiceEvents();
            log = LogManager.GetLogger(GetType());
        }

        public async void Register(IAppHost appHost)
        {
            var connection = EventStoreConnection.Create(connectionSettings.GetConnectionString());

            await connection.ConnectAsync().ConfigureAwait(false); //no need for the initial synchronisation context 
                                                                   //to be reused when executing the rest of the method

            new ConnectionMonitor(connection, connectionSettings.MonitorSettings)
                    .AddHandlers();

            container = appHost.GetContainer();

            RegisterTypesForIoc(connection);

            appHost.GetPlugin<MetadataFeature>()?
                   .AddPluginLink($"http://{connectionSettings.GetHttpEndpoint()}/", "EventStore");

            try
            {
                foreach (var subscription in featureSettings.Subscriptions)
                {
                    var consumer = (StreamConsumer)container.TryResolve(consumers[subscription.GetType().Name]);
                    await consumer.ConnectToSubscription(subscription).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

        private void RegisterTypesForIoc(IEventStoreConnection connection)
        {
            container.RegisterAutoWired<PersistentConsumer>();
            container.RegisterAutoWired<CatchUpConsumer>();
            container.RegisterAutoWired<VolatileConsumer>();
            container.RegisterAutoWired<ReadModelConsumer>();
            container.RegisterAutoWiredAs<EventStoreRepository, IEventStoreRepository>();
            container.RegisterAutoWiredAs<EventDispatcher, IEventDispatcher>().ReusedWithin(ReuseScope.Default);
            container.Register(c => connection).ReusedWithin(ReuseScope.Container);

            RegisterStorageTypes();
        }

        private void RegisterStorageTypes()
        {
            var readModelDelegates = new Dictionary<StorageType, Action<string>>
            {
                {StorageType.Redis, (cs) => container.Register<IRedisClientsManager>(c => new RedisManagerPool(cs))}
            };

            var readModel = featureSettings.ReadModel();
            readModelDelegates[readModel.StorageType]?.Invoke(readModel.ConnectionString);
        }
    }
}
