﻿using System;
using System.Collections.Generic;
using System.Security.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using WampSharp.Binding;
using WampSharp.Tests.TestHelpers.Integration;
using WampSharp.V2;
using WampSharp.V2.Client;
using WampSharp.V2.Core.Contracts;

namespace WampSharp.Tests.Wampv2.Integration
{
    [TestFixture]
    public class AuthenticationTests
    {
        [Test]
        public void AuthenticatorSendsDetailsToHello()
        {
            WampClientPlayground playground =
                new WampClientPlayground();

            CustomAuthenticator authenticator =
                new CustomAuthenticator
                {
                    AuthenticationId = "peter",
                    AuthenticationMethods = new string[] {"ticket"}
                };

            HelloMock mock = new HelloMock();

            JTokenJsonBinding jsonBinding = new JTokenJsonBinding();

            IWampChannel channel =
                playground.GetChannel
                    (mock,
                        "realm1",
                        jsonBinding,
                        authenticator);

            channel.Open();

            IDictionary<string, ISerializedValue> deserializedDetails =
                jsonBinding.Formatter.Deserialize<IDictionary<string, ISerializedValue>>
                    (mock.Details);

            Assert.That(deserializedDetails["authmethods"].Deserialize<string[]>(),
                Is.EquivalentTo(authenticator.AuthenticationMethods));

            Assert.That(deserializedDetails["authid"].Deserialize<string>(),
                Is.EquivalentTo(authenticator.AuthenticationId));
        }

        [Test]
        public void AuthenticatorGetsChallengeMessage()
        {
            WampClientPlayground playground = new WampClientPlayground();

            CustomAuthenticator authenticator =
                new CustomAuthenticator
                {
                    AuthenticationId = "peter",
                    AuthenticationMethods = new string[] { "ticket" }
                };

            MyChallengeDetails myChallengeDetails = new MyChallengeDetails()
            {
                MyNumber = 3
            };

            ChallengeMock mock =
                new ChallengeMock("ticket",
                    myChallengeDetails);

            JTokenJsonBinding jsonBinding = new JTokenJsonBinding();

            IWampChannel channel =
                playground.GetChannel
                    (mock,
                        "realm1",
                        jsonBinding,
                        authenticator);

            channel.Open();

            Assert.That(authenticator.AuthMethod, Is.EqualTo("ticket"));
            
            Assert.That(authenticator.Extra.OriginalValue.Deserialize<MyChallengeDetails>(),
                Is.EqualTo(myChallengeDetails));
        }

        [Test]
        public void AuthenticatorAuthenticateResultCallsAuthenticate()
        {
            WampClientPlayground playground = new WampClientPlayground();

            CustomAuthenticator authenticator =
                new CustomAuthenticator
                    (delegate
                    {
                        return new AuthenticationResponse()
                        {
                            Extra = new Dictionary<string, object>()
                            {
                                {"secret1", 3}
                            },
                            Signature = "secretsignature"
                        };
                    })
                {
                    AuthenticationId = "peter",
                    AuthenticationMethods = new string[] {"ticket"}
                };

            AuthenticateMock mock = new AuthenticateMock("ticket");

            JTokenJsonBinding jsonBinding = new JTokenJsonBinding();

            IWampChannel channel =
                playground.GetChannel
                    (mock,
                        "realm1",
                        jsonBinding,
                        authenticator);

            channel.Open();

            Assert.That(mock.Signature,
                Is.EqualTo("secretsignature"));

            IDictionary<string, ISerializedValue> deserializedExtra = 
                jsonBinding.Formatter.Deserialize<IDictionary<string, ISerializedValue>>(mock.Extra);

            Assert.That(deserializedExtra["secret1"].Deserialize<int>(),
                Is.EqualTo(3));
        }

        [Test]
        public void AuthenticatorAuthenticateExceptionCallsAbort()
        {
            WampClientPlayground playground = new WampClientPlayground();

            MyAbortDetails myAbortDetails = new MyAbortDetails()
            {
                Message = "My message",
                User = "Joe"
            };

            CustomAuthenticator authenticator =
                new CustomAuthenticator
                    (delegate
                    {
                        throw new WampAuthenticationException(myAbortDetails, "some reason");
                    })
                {
                    AuthenticationId = "peter",
                    AuthenticationMethods = new string[] { "ticket" }
                };

            AbortMock mock = new AbortMock("ticket");

            JTokenJsonBinding jsonBinding = new JTokenJsonBinding();

            IWampChannel channel =
                playground.GetChannel
                    (mock,
                        "realm1",
                        jsonBinding,
                        authenticator);

            channel.Open();

            Assert.That(mock.Reason,
                Is.EqualTo("some reason"));

            MyAbortDetails deserializedDetails =
                jsonBinding.Formatter.Deserialize<MyAbortDetails>(mock.Details);

            Assert.That(deserializedDetails, Is.EqualTo(myAbortDetails));
        }

        private class MyAbortDetails : AbortDetails
        {
            [JsonProperty("user")]
            public string User { get; set; }

            protected bool Equals(MyAbortDetails other)
            {
                return string.Equals(User, other.User) &&
                    string.Equals(Message, other.Message);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((MyAbortDetails) obj);
            }

            public override int GetHashCode()
            {
                return (User != null ? User.GetHashCode() : 0);
            }
        }

        private class AbortMock : ChallengeMock
        {
            public JToken Details { get; private set; }
            public string Reason { get; private set; }

            public AbortMock(string authMethod) : base(authMethod)
            {
            }

            public override void Abort(IWampSessionClient client, JToken details, string reason)
            {
                Reason = reason;
                Details = details;
            }
        }

        private class AuthenticateMock : ChallengeMock
        {
            public string Signature { get; private set; }
            public JToken Extra { get; private set; }

            public AuthenticateMock(string authMethod) : base(authMethod)
            {
            }

            public override void Authenticate(IWampSessionClient client, string signature, JToken extra)
            {
                Extra = extra;
                Signature = signature;
            }
        }

        private class ChallengeMock : MockServer<JToken>
        {
            private readonly string mAuthMethod;
            private readonly ChallengeDetails mDetails;

            public ChallengeMock(string authMethod) : this(authMethod, new ChallengeDetails())
            {
            }

            public ChallengeMock(string authMethod, ChallengeDetails details)
            {
                mAuthMethod = authMethod;
                mDetails = details;
            }

            public override void Hello(IWampSessionClient client, string realm, JToken details)
            {
                client.Challenge(mAuthMethod, mDetails);
            }
        }

        private class HelloMock : HelloMock<JToken>
        {
        }

        private class HelloMock<TMessage> : MockServer<TMessage>
        {
            private TMessage mDetails;

            public TMessage Details
            {
                get
                {
                    return mDetails;
                }
            }

            public override void Hello(IWampSessionClient client, string realm, TMessage details)
            {
                mDetails = details;
            }
        }

        private class MockServer<TMessage> : IWampServer<TMessage>
        {

            public virtual void Hello(IWampSessionClient client, string realm, TMessage details)
            {
            }

            public virtual void Abort(IWampSessionClient client, TMessage details, string reason)
            {
            }

            public virtual void Authenticate(IWampSessionClient client, string signature, TMessage extra)
            {
            }

            public void Goodbye(IWampSessionClient client, TMessage details, string reason)
            {
            }

            public void Heartbeat(IWampSessionClient client, int incomingSeq, int outgoingSeq)
            {
                throw new NotImplementedException();
            }

            public void Heartbeat(IWampSessionClient client, int incomingSeq, int outgoingSeq, string discard)
            {
                throw new NotImplementedException();
            }

            public void OnNewClient(IWampClient<TMessage> client)
            {
                throw new NotImplementedException();
            }

            public void OnClientDisconnect(IWampClient<TMessage> client)
            {
                throw new NotImplementedException();
            }

            public void Register(IWampCallee callee, long requestId, RegisterOptions options, string procedure)
            {
                throw new NotImplementedException();
            }

            public void Unregister(IWampCallee callee, long requestId, long registrationId)
            {
                throw new NotImplementedException();
            }

            public void Call(IWampCaller caller, long requestId, CallOptions options, string procedure)
            {
                throw new NotImplementedException();
            }

            public void Call(IWampCaller caller, long requestId, CallOptions options, string procedure, TMessage[] arguments)
            {
                throw new NotImplementedException();
            }

            public void Call(IWampCaller caller, long requestId, CallOptions options, string procedure, TMessage[] arguments,
                IDictionary<string, TMessage> argumentsKeywords)
            {
                throw new NotImplementedException();
            }

            public void Cancel(IWampCaller caller, long requestId, CancelOptions options)
            {
                throw new NotImplementedException();
            }

            public void Yield(IWampCallee callee, long requestId, YieldOptions options)
            {
                throw new NotImplementedException();
            }

            public void Yield(IWampCallee callee, long requestId, YieldOptions options, TMessage[] arguments)
            {
                throw new NotImplementedException();
            }

            public void Yield(IWampCallee callee, long requestId, YieldOptions options, TMessage[] arguments, IDictionary<string, TMessage> argumentsKeywords)
            {
                throw new NotImplementedException();
            }

            public void Error(IWampClient client, int requestType, long requestId, TMessage details, string error)
            {
                throw new NotImplementedException();
            }

            public void Error(IWampClient client, int requestType, long requestId, TMessage details, string error, TMessage[] arguments)
            {
                throw new NotImplementedException();
            }

            public void Error(IWampClient client, int requestType, long requestId, TMessage details, string error, TMessage[] arguments,
                TMessage argumentsKeywords)
            {
                throw new NotImplementedException();
            }

            public void Publish(IWampPublisher publisher, long requestId, PublishOptions options, string topicUri)
            {
                throw new NotImplementedException();
            }

            public void Publish(IWampPublisher publisher, long requestId, PublishOptions options, string topicUri, TMessage[] arguments)
            {
                throw new NotImplementedException();
            }

            public void Publish(IWampPublisher publisher, long requestId, PublishOptions options, string topicUri, TMessage[] arguments,
                IDictionary<string, TMessage> argumentKeywords)
            {
                throw new NotImplementedException();
            }

            public void Subscribe(IWampSubscriber subscriber, long requestId, SubscribeOptions options, string topicUri)
            {
                throw new NotImplementedException();
            }

            public void Unsubscribe(IWampSubscriber subscriber, long requestId, long subscriptionId)
            {
                throw new NotImplementedException();
            }
        }

        private class CustomAuthenticator : IWampClientAuthenticator
        {
            private string mAuthMethod;
            private ChallengeDetails mExtra;
            private readonly Func<string, ChallengeDetails, AuthenticationResponse> mAuthenticate;

            public CustomAuthenticator() : 
                this((authMethod, extra) => new AuthenticationResponse())
            {
            }

            public CustomAuthenticator(Func<string, ChallengeDetails, AuthenticationResponse> authenticate)
            {
                mAuthenticate = authenticate;
            }

            public AuthenticationResponse Authenticate(string authmethod, ChallengeDetails extra)
            {
                mExtra = extra;
                mAuthMethod = authmethod;
                return mAuthenticate(authmethod, extra);
            }

            public string[] AuthenticationMethods { get; set; }

            public string AuthenticationId { get; set; }

            public string AuthMethod
            {
                get { return mAuthMethod; }
                set { mAuthMethod = value; }
            }

            public ChallengeDetails Extra
            {
                get { return mExtra; }
                set { mExtra = value; }
            }
        }

        private class MyChallengeDetails : ChallengeDetails
        {
            [JsonProperty("number")]
            public int MyNumber { get; set; }

            protected bool Equals(MyChallengeDetails other)
            {
                return MyNumber == other.MyNumber;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((MyChallengeDetails) obj);
            }

            public override int GetHashCode()
            {
                return MyNumber;
            }
        }
    }

}