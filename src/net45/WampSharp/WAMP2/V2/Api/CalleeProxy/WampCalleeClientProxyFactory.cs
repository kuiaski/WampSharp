﻿using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WampSharp.Core.Listener;
using WampSharp.V2.Client;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Realm;
using WampSharp.V2.Rpc;

namespace WampSharp.V2.CalleeProxy
{
    internal class WampCalleeClientProxyFactory : WampCalleeProxyFactory
    {
        public WampCalleeClientProxyFactory(IWampRpcOperationCatalogProxy catalogProxy, IWampClientConnectionMonitor monitor) : 
            base(new ClientInvocationHandler(catalogProxy, monitor))
        {
        }

        private class ClientInvocationHandler : WampCalleeProxyInvocationHandler
        {
            #region Data Members

            private readonly IWampRpcOperationCatalogProxy mCatalogProxy;
            private readonly IWampClientConnectionMonitor mMonitor;

            private TaskCompletionSource<object> mDisconnectionTaskCompletionSource;

            private ManualResetEvent mDisconnectionWaitHandle;

            private Exception mDisconnectionException;
            private readonly CallOptions mEmptyOptions = new CallOptions();

            #endregion

            #region Constructor

            public ClientInvocationHandler(IWampRpcOperationCatalogProxy catalogProxy,
                IWampClientConnectionMonitor monitor)
            {
                mCatalogProxy = catalogProxy;
                mMonitor = monitor;
                mDisconnectionTaskCompletionSource = new TaskCompletionSource<object>();
                mDisconnectionWaitHandle = new ManualResetEvent(false);                

                mMonitor.ConnectionEstablished += OnConnectionEstablished;
                mMonitor.ConnectionError += OnConnectionError;
                mMonitor.ConnectionBroken += OnConnectionBroken;
            }

            #endregion

            #region Private Methods

            private void OnConnectionEstablished(object sender, WampSessionEventArgs e)
            {
                mDisconnectionTaskCompletionSource = new TaskCompletionSource<object>();
                mDisconnectionWaitHandle = new ManualResetEvent(false);                
            }

            private void OnConnectionBroken(object sender, WampSessionCloseEventArgs e)
            {
                Exception exception = new WampConnectionBrokenException(e);
                SetException(exception);
            }

            private void OnConnectionError(object sender, WampConnectionErrorEventArgs e)
            {
                Exception exception = e.Exception;
                SetException(exception);
            }

            private void SetException(Exception exception)
            {
                mDisconnectionException = exception;
                mDisconnectionTaskCompletionSource.TrySetException(exception);
                mDisconnectionWaitHandle.Set();
            }

            #endregion

            #region Overridden

#if NET45
            protected async override Task<object> AwaitForResult(AsyncOperationCallback asyncOperationCallback)
#else
            protected override Task<object> AwaitForResult(AsyncOperationCallback asyncOperationCallback)
#endif
            {
#if NET45
                Task<object> task = await Task.WhenAny(asyncOperationCallback.Task,
                                                       mDisconnectionTaskCompletionSource.Task);

                object result = await task;

                return result;
#else
                IObservable<object> merged =
                    Observable.Amb(asyncOperationCallback.Task.ToObservable(),
                                     mDisconnectionTaskCompletionSource.Task.ToObservable());
                
                Task<object> task = merged.ToTask();

                return task;
#endif
            }

            protected override void WaitForResult(SyncCallback callback)
            {
                int signaledIndex =
                    WaitHandle.WaitAny(new[] {mDisconnectionWaitHandle, callback.WaitHandle},
                                   Timeout.Infinite);


                if (signaledIndex == 0)
                {
                    callback.SetException(mDisconnectionException);
                }

                base.WaitForResult(callback);
            }

            protected override void Invoke(ICalleeProxyInterceptor interceptor, IWampRawRpcOperationClientCallback callback, MethodInfo method, object[] arguments)
            {
                CallOptions callOptions = interceptor.GetCallOptions(method);
                var procedureUri = interceptor.GetProcedureUri(method);

                mCatalogProxy.Invoke(callback,
                                     callOptions,
                                     procedureUri,
                                     arguments);
            }

            #endregion
        }
    }
}