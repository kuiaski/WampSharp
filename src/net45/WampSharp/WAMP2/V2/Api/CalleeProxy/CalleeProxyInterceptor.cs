using System.Reflection;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Rpc;

// ReSharper disable once CheckNamespace
namespace WampSharp.V2
{
    public class CalleeProxyInterceptor : ICalleeProxyInterceptor
    {
        private readonly CallOptions mCallOptions;

        public static readonly ICalleeProxyInterceptor Default =
            new CachedCalleeProxyInterceptor
                (new CalleeProxyInterceptor
                    (new CallOptions()));

        public CalleeProxyInterceptor() : this(new CallOptions())
        {
        }

        public CalleeProxyInterceptor(CallOptions callOptions)
        {
            mCallOptions = callOptions;
        }

        public virtual CallOptions GetCallOptions(MethodInfo method)
        {
            CallOptions result = new CallOptions(mCallOptions);

#if !NET40
            if (method.IsDefined(typeof (WampProgressiveResultProcedureAttribute)))
            {
                result.ReceiveProgress = true;
            }
#endif

            return result;
        }

        public virtual string GetProcedureUri(MethodInfo method)
        {
            WampProcedureAttribute attribute = 
                method.GetCustomAttribute<WampProcedureAttribute>();

            return attribute.Procedure;
        }
    }
}