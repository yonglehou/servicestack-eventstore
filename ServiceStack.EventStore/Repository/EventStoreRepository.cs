﻿using EventStore.ClientAPI;

namespace ServiceStack.EventStore.Repository
{
    using Types;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Idempotency;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Exceptions;
    using Text;

    public delegate string GetStreamName(Type type, Guid guid);

    public class EventStoreRepository : IEventStoreRepository
    {
        private const string EventClrTypeHeader = "EventClrTypeName";
        private const string AggregateClrTypeHeader = "AggregateClrTypeName";
        private const int WritePageSize = 500;
        private const int ReadPageSize = 500;

        private readonly GetStreamName getStreamName;
        private readonly IEventStoreConnection connection;

        public EventStoreRepository(IEventStoreConnection connection)
        {
            this.connection = connection;
            getStreamName = (type, guid) => $"{type.Name}-{guid}"; //todo make this a delegate
        }

        public async void Publish(Event @event)
        {
            var streamName = @event.StreamName;

            var headers = new Dictionary<string, object>
                {
                    {EventClrTypeHeader, @event.GetType().Name}
                };

            await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, ToEventData(@event, headers));
        }

        public async void Publish(EventSourcedAggregate eventSourcedAggregate)
        {
            var headers = new Dictionary<string, object>
                {
                    {AggregateClrTypeHeader, eventSourcedAggregate.GetType().Name}
                };

            var streamName = getStreamName(eventSourcedAggregate.GetType(), eventSourcedAggregate.Id);

            var newEvents = eventSourcedAggregate.Changes.ToList();
            var originalVersion = eventSourcedAggregate.Version - newEvents.Count;
            var expectedVersion = originalVersion == 0 
                                    ? ExpectedVersion.NoStream 
                                    : originalVersion - 1;
            var eventsToSave = newEvents.Select(@event => ToEventData(@event, headers)).ToList();

            if (eventsToSave.Count < WritePageSize)
            {
                try
                {
                    await connection.AppendToStreamAsync(streamName, expectedVersion, eventsToSave);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                var transaction = await connection.StartTransactionAsync(streamName, expectedVersion);
                var position = 0;

                while (position < eventsToSave.Count)
                {
                    var pageEvents = eventsToSave.Skip(position).Take(WritePageSize);
                    try
                    {
                        await transaction.WriteAsync(pageEvents);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    position += WritePageSize;
                }

                await transaction.CommitAsync();
            }
        }

        public async Task<TAggregate> GetById<TAggregate>(Guid id, int version) where TAggregate : EventSourcedAggregate
        {
            if (version <= 0)
                throw new InvalidOperationException("Cannot get version <= 0");

            var streamName = getStreamName(typeof(TAggregate), id);
            var aggregate = ConstructAggregate<TAggregate>();

            var sliceStart = 1; //Ignores $StreamCreated
            StreamEventsSlice currentSlice;

            do
            {
                var sliceCount = sliceStart + ReadPageSize <= version
                                    ? ReadPageSize
                                    : version - sliceStart + 1;

                currentSlice = await connection.ReadStreamEventsForwardAsync(streamName, sliceStart, sliceCount, false);

                if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                    throw new AggregateNotFoundException(id, typeof(TAggregate));

                if (currentSlice.Status == SliceReadStatus.StreamDeleted)
                    throw new AggregateDeletedException(id, typeof(TAggregate));

                sliceStart = currentSlice.NextEventNumber;

                foreach (var evnt in currentSlice.Events)
                    aggregate.ApplyEvent((IDomainEvent)DeserializeEvent(evnt.OriginalEvent.Metadata, evnt.OriginalEvent.Data));

            } while (version >= currentSlice.NextEventNumber && !currentSlice.IsEndOfStream);

            if (aggregate.Version != version && version < int.MaxValue)
                throw new AggregateVersionException(id, typeof(TAggregate), aggregate.Version, version);

            return aggregate;
        }

        private static object DeserializeEvent(byte[] metadata, byte[] data)
        {
            var eventClrTypeName = JsonObject.Parse(metadata.FromAsciiBytes()).GetUnescaped(EventClrTypeHeader);
            var serializer = new JsonStringSerializer();

            return serializer.DeserializeFromString(data.FromAsciiBytes(), Type.GetType(eventClrTypeName));
        }

        private static TAggregate ConstructAggregate<TAggregate>()
        {
            return (TAggregate)Activator.CreateInstance(typeof(TAggregate), true);
        }

        private EventData ToEventData(object @event, IDictionary<string, object> headers)
        {
            var json = @event.ToJson();
            var data = json.ToAsciiBytes();
            var deterministicEventId = GetDeterministicEventId(json);
            var typeName = @event.GetType().Name;

            var eventHeaders = new Dictionary<string, object>(headers)
            {
                {
                    EventClrTypeHeader, @event.GetType().AssemblyQualifiedName
                }
            };

            var metadata = eventHeaders.ToJson().ToAsciiBytes();

            return new EventData(deterministicEventId, typeName, true, data, metadata);
        }

        private Guid GetExecutingAssemblyGuid()
        {
            var assembly = GetType().Assembly;
            var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
            var assemblyGuid = new Guid(attribute.Value);
            return assemblyGuid;
        }

        private Guid GetDeterministicEventId(string json)
        {
            var assemblyGuid = GetExecutingAssemblyGuid();
            var deterministicEventId = GuidUtility.Create(assemblyGuid, json);
            return deterministicEventId;
        }
    }
}