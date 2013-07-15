//
//      Copyright (C) 2012 DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
using System.Net;
using System.Text;
using System;

namespace Cassandra
{
    /// <summary>
    ///  AuthProvider which supplies authenticator instances for clients to connect to
    ///  DSE clusters secured with Kerberos. See <link>KerberosAuthenticator</link>
    ///  for how to configure client side Kerberos options. To connect to clusters
    ///  using internal authentication, use the standard method for setting
    ///  credentials. eg: <pre> Cluster cluster = Cluster.builder()
    ///  .addContactPoint(hostname) .withCredentials("username", "password") .build();
    ///  </pre>
    /// </summary>

    public class DseAuthProvider : IAuthProvider
    {
        public IAuthenticator NewAuthenticator(IPAddress host)
        {
            return new KerberosAuthenticator(host);
        }
    }
}