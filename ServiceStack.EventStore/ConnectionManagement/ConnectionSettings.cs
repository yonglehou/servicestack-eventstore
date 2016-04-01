﻿using EventStore.ClientAPI;

namespace ServiceStack.EventStore.ConnectionManagement
{
    using System.Collections.Generic;
    using System.Text;
    using FluentValidation;

    /// <summary>
    /// Enables the developer to specify the connection settings to the running EventStore instance.
    /// </summary>
    public class EventStoreConnectionSettings
    {
        public MonitorSettings MonitorSettings { get; set; }
        private readonly Dictionary<string, object> settings = new Dictionary<string, object>();
        private readonly Validator validator = new Validator();

        private string httpEndpoint;
        private string tcpEndpoint;
        private string userName = "";
        private string password = "";

        public EventStoreConnectionSettings()
        {
            MonitorSettings = new MonitorSettings();
        }

        private class Validator : AbstractValidator<EventStoreConnectionSettings>
        {
            public Validator()
            {
                RuleFor(cs => cs.userName).NotEmpty();
                RuleFor(cs => cs.password).NotEmpty();
                RuleFor(cs => cs.tcpEndpoint).NotEmpty();
            }
        }

        public string GetConnectionString()
        {
            validator.ValidateAndThrow(this);

            var connectionString = new StringBuilder();
            connectionString.Append($"ConnectTo=tcp://{userName}:{password}@{tcpEndpoint}; ");
            settings.Each(s => connectionString.Append($"{s.Key}={s.Value}; "));
            return connectionString.ToString();
        }

        public string GetHttpEndpoint()
        {
            return httpEndpoint;
        }

        public EventStoreConnectionSettings HttpEndpoint(string endpoint)
        {
            httpEndpoint = endpoint;
            return this;
        }

        public string GetTcpEndpoint()
        {
            return tcpEndpoint;
        }

        public EventStoreConnectionSettings TcpEndpoint(string endpoint)
        {
            tcpEndpoint = endpoint;
            return this;
        }

        public EventStoreConnectionSettings UserName(string name)
        {
            userName = name;
            return this;
        }

        public EventStoreConnectionSettings Password(string pwd)
        {
            password = pwd;
            return this;
        }

        public EventStoreConnectionSettings ReconnectionDelay(int delay)
        {
            settings["ReconnectionDelay"] = delay;
            return this;
        }

        public EventStoreConnectionSettings HeartbeatTimeout(int timeout)
        {
            settings["HeartbeatTimeout"] = timeout;
            return this;
        }

        public EventStoreConnectionSettings MaxReconnections(int reconnections)
        {
            settings["MaxReconnections"] = reconnections;
            return this;
        }
    }
}
