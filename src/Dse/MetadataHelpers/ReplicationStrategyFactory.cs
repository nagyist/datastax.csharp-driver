﻿// 
//       Copyright DataStax, Inc.
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//       http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// 

using System;
using System.Collections.Generic;

namespace Dse.MetadataHelpers
{
    internal class ReplicationStrategyFactory : IReplicationStrategyFactory
    {
        private static readonly Logger Logger = new Logger(typeof(ReplicationStrategyFactory));

        public IReplicationStrategy Create(string strategyClass, IReadOnlyDictionary<string, int> replicationOptions)
        {
            if (strategyClass.Equals(ReplicationStrategies.SimpleStrategy, StringComparison.OrdinalIgnoreCase))
            {
                return replicationOptions.TryGetValue("replication_factor", out var replicationFactorValue)
                    ? new SimpleStrategy(replicationFactorValue)
                    : null;
            }

            if (strategyClass.Equals(ReplicationStrategies.NetworkTopologyStrategy, StringComparison.OrdinalIgnoreCase)) 
            {
                return new NetworkTopologyStrategy(replicationOptions);
            }

            ReplicationStrategyFactory.Logger.Info($"Replication Strategy class name not recognized: {strategyClass}");

            return null;
        }
    }
}