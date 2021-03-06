﻿using Castle.DynamicProxy;
using WampSharp.V2.Core.Contracts;

namespace WampSharp.V2.CalleeProxy
{
    internal class WampCalleeProxyFactory : IWampCalleeProxyFactory
    {
        private readonly ProxyGenerator mGenerator = new ProxyGenerator();
        private readonly WampCalleeProxyInvocationHandler mHandler;

        public WampCalleeProxyFactory(WampCalleeProxyInvocationHandler handler)
        {
            mHandler = handler;
        }

        public TProxy GetProxy<TProxy>(ICalleeProxyInterceptor interceptor) where TProxy : class
        {
            ProxyGenerationOptions options = new ProxyGenerationOptions()
            {
                Selector = new WampCalleeProxyInterceptorSelector(mHandler, interceptor)
            };

            TProxy proxy =
                mGenerator.CreateInterfaceProxyWithoutTarget<TProxy>
                    (options,
                        new IInterceptor[]
                        {
                            new SyncCalleeProxyInterceptor(mHandler, interceptor),
                            new AsyncCalleeProxyInterceptor(mHandler, interceptor)
                        });

            return proxy;
        }
    }
}