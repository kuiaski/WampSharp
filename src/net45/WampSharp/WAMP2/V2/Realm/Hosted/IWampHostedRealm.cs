﻿using System;

namespace WampSharp.V2.Realm
{
    /// <summary>
    /// Represents a <see cref="IWampRealm"/> which is hosted in a <see cref="IWampHost"/>.
    /// </summary>
    public interface IWampHostedRealm : IWampRealm
    {
        /// <summary>
        /// Occurs when a new session is created.
        /// </summary>
        event EventHandler<WampSessionEventArgs> SessionCreated;

        /// <summary>
        /// Occurs when a session is closed.
        /// </summary>
        event EventHandler<WampSessionCloseEventArgs> SessionClosed;

        /// <summary>
        /// Gets the services associated with this realm. 
        /// </summary>
        IWampRealmServiceProvider Services { get; }

        /// <summary>
        /// Gets the session associated with this hosted realm internal client.
        /// </summary>
        long SessionId { get; }
    }
}