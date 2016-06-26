﻿using System;
using System.Collections.Generic;
using Ninject;

namespace VMLib.IOC
{
    public class ServiceDiscovery : IServiceDiscovery
    {
        private static IServiceDiscovery _serviceDiscovery;

        public static IServiceDiscovery Instance => _serviceDiscovery ?? (_serviceDiscovery = new ServiceDiscovery());

        private readonly StandardKernel _container = new StandardKernel();

        internal static void UnitTestInjection(IServiceDiscovery fakeobject)
        {
            _serviceDiscovery = fakeobject;
        }

        public IEnumerable<T> ResolveAll<T>()
        {
            return _container.GetAll<T>();
        }

        public void AddType<T, T1>()
        {
            _container.Bind(typeof(T)).To(typeof(T1));
        }

        public void AddType<T, T1>(string name)
        {
            _container.Bind(typeof(T)).To(typeof(T1)).Named(name);
        }

        public void AddType(Type Interface, Type concrete)
        {
            _container.Bind(Interface).To(concrete);
        }

        public T Resolve<T>(string name)
        {
            return _container.Get<T>(name);
        }

        public T Resolve<T>()
        {
            return _container.Get<T>();
        }

        public void AddSingletonType<T, T1>(string name)
        {
            if(!_container.CanResolve<T>(name))
                _container.Bind(typeof(T)).To(typeof(T1)).InSingletonScope().Named(name);
        }
    }
}
