﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.EventStore.Projections
{
    using System;
    using System.Threading.Tasks;
    using Redis;
    using TaskExtensions = Extensions.TaskExtensions;

    /// <summary>
    /// Implementation of IProjectionWriter that uses the ServiceStack.RedisClient
    /// Based on: https://github.com/gnschenker/EventSourcing/blob/master/src/ReadModelBuilder/MongoDbProjectionWriter.cs
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TViewModel"></typeparam>
    public class RedisReadModelWriter<TId, TViewModel> : IReadModelWriter<TId, TViewModel> 
                                                            where TId : struct 
                                                            where TViewModel : class
    {
        private readonly IRedisClient redisClient;

        public RedisReadModelWriter(IRedisClientsManager redis)
        {
            redisClient = redis.GetClient();
        }

        public Task Add(TViewModel item)
        {
            var typedClient = redisClient.As<TViewModel>();
            typedClient.Store(item);

            return TaskExtensions.CompletedTask;
        }

        public Task Update(TId id, Action<TViewModel> update)
        {
            var typedClient = redisClient.As<TViewModel>();
            var viewModel = typedClient.GetById(id);

            if (viewModel == null)
                throw new InvalidOperationException($"View Model {typeof(TViewModel).Name} with Id {id} cannot be found.");

            update(viewModel);
            typedClient.Store(viewModel);

            return TaskExtensions.CompletedTask;
        }
    }
}
