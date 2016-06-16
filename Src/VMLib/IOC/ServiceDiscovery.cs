﻿using System.Collections.Generic;

namespace VMLib.IOC
{
    public class ServiceDiscovery : IServiceDiscovery
    {
        private static IServiceDiscovery _serviceDiscovery;

        public static IServiceDiscovery Instance => _serviceDiscovery ?? (_serviceDiscovery = new ServiceDiscovery());

        internal static void UnitTestInjection(IServiceDiscovery fakeobject)
        {
            _serviceDiscovery = fakeobject;
        }

        public IEnumerable<T> ResolveAll<T>()
        {
            throw new System.NotImplementedException();
        }
    }
}
