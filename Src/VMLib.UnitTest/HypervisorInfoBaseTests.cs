﻿using System;
using FakeItEasy;
using NUnit.Framework;
using VMLib.IOC;

namespace VMLib.UnitTest
{


    [TestFixture]
    public class HypervisorInfoBaseTests 
    {
        public IServiceDiscovery DefaultServiceDiscovery()
        {
            var srv = A.Fake<IServiceDiscovery>();
            ServiceDiscovery.UnitTestInjection(srv);
            return srv;
        }

        public TesableHyperVisorBase DefaultTestableHypervisorInfoBase()
        {
            var sut = new TesableHyperVisorBase();

            return sut;

        }

        [Test]
        public void AddTypeToIOC_AddingATypeToIOC_CallsServiceDiscovery()
        {
            var srv = DefaultServiceDiscovery();
            var sut = DefaultTestableHypervisorInfoBase();
            
            sut.TestInternal_AddTypeToIOC<IHypervisor, FakeHypervisor>();

            A.CallTo(() => srv.AddType<IHypervisor, FakeHypervisor>("Testable")).MustHaveHappened();
        }

        [Test]
        public void CreateHypervisor_CreateInstance_CallsServiceDiscoveryToRetrive()
        {
            var srv = DefaultServiceDiscovery();
            var sut = DefaultTestableHypervisorInfoBase();

            var result = sut.CreateHypervisor(A.Fake<IHypervisorConnectionInfo>());

            A.CallTo(() => srv.Resolve<IHypervisor>(sut.Name)).MustHaveHappened();
        }

        [Test]
        public void CreateHypervisor_AddingPropertiesToHypervisor_HypervisorSetConnectionSettingsIsCalled()
        {
            var srv = DefaultServiceDiscovery();
            var sut = DefaultTestableHypervisorInfoBase();
            var hypervisor = A.Fake<IHypervisor>();

            A.CallTo(() => srv.Resolve<IHypervisor>(sut.Name)).Returns(hypervisor);

            var result = sut.CreateHypervisor(A.Fake<IHypervisorConnectionInfo>());

            A.CallTo(() => hypervisor.SetConnectionSettings(A<IHypervisorConnectionInfo>.Ignored)).MustHaveHappened();
        }

        [Test]
        public void CreateHypervisor_PassingNullAsConnectionInfo_Throws()
        {
            var sut = DefaultTestableHypervisorInfoBase();

            Assert.Throws<ArgumentNullException>(() => sut.CreateHypervisor(null));
        }
    }

    public class FakeHypervisor : IHypervisor
    {
        public void SetConnectionSettings(IHypervisorConnectionInfo settings)
        {
            throw new NotImplementedException();
        }
    }

    public class TesableHyperVisorBase : HypervisorInfoBase
    {
        public override string Name => "Testable";

        public void TestInternal_AddTypeToIOC<F, T>()
        {
            AddTypeToIOC<F,T>();
        }

        public override IHypervisorConnectionInfo CreateConnectionInfo()
        {
            throw new NotImplementedException();
        }
    }
}
