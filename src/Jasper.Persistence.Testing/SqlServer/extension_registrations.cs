﻿using System.Data.Common;
using System.Data.SqlClient;
using IntegrationTests;
using Jasper.Messaging.Durability;
using Jasper.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Jasper.Persistence.Testing.SqlServer
{
    public class extension_registrations : SqlServerContext
    {
        [Fact]
        public void registrations()
        {
            using (var runtime = JasperHost.For(x =>
                x.Settings.PersistMessagesWithSqlServer(Servers.SqlServerConnectionString)))
            {
                runtime.Container.Model.HasRegistrationFor<SqlConnection>().ShouldBeTrue();
                runtime.Container.Model.HasRegistrationFor<DbConnection>().ShouldBeTrue();

                runtime.Container.Model.For<SqlConnection>().Default.Lifetime.ShouldBe(ServiceLifetime.Scoped);


                runtime.Container.Model.HasRegistrationFor<IEnvelopePersistence>().ShouldBeTrue();


                runtime.Get<SqlConnection>().ConnectionString.ShouldBe(Servers.SqlServerConnectionString);
                runtime.Get<DbConnection>().ShouldBeOfType<SqlConnection>()
                    .ConnectionString.ShouldBe(Servers.SqlServerConnectionString);
            }
        }
    }
}
