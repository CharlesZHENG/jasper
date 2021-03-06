﻿using Baseline;
using IntegrationTests;
using Jasper.Messaging.Durability;
using Jasper.Persistence.SqlServer;
using Jasper.Persistence.SqlServer.Schema;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Jasper.Persistence.Testing.SqlServer
{
    public class SqlServerEnvelopeStorageAdminTests : SqlServerContext
    {
        private readonly ITestOutputHelper _output;

        public SqlServerEnvelopeStorageAdminTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void drop_then_create()
        {
            var loader = new SqlServerEnvelopeStorageAdmin(new SqlServerSettings{ConnectionString = Servers.SqlServerConnectionString});
            loader.DropAll();

            loader.CreateAll();
        }

        [Fact]
        public void generate_sql()
        {
            var loader = new SqlServerEnvelopeStorageAdmin(new SqlServerSettings{ConnectionString = Servers.SqlServerConnectionString});
            _output.WriteLine(loader.As<IEnvelopeStorageAdmin>().CreateSql());
        }

        [Fact]
        public void drop_then_create_different_schema()
        {
            var loader = new SqlServerEnvelopeStorageAdmin(new SqlServerSettings{ConnectionString = Servers.SqlServerConnectionString, SchemaName = "receiver"});
            loader.DropAll();

            loader.CreateAll();
        }

        [Fact]
        public void recreate_all_tables()
        {
            var loader = new SqlServerEnvelopeStorageAdmin(new SqlServerSettings{ConnectionString = Servers.SqlServerConnectionString});
            loader.RecreateAll();
        }

        [Fact]
        public void recreate_all_tables_in_a_different_schema()
        {
            var loader = new SqlServerEnvelopeStorageAdmin(new SqlServerSettings{ConnectionString = Servers.SqlServerConnectionString, SchemaName = "sender"});
            loader.RecreateAll();
        }

        [Fact]
        public void smoke_test_clear_all()
        {
            var loader = new SqlServerEnvelopeStorageAdmin(new SqlServerSettings{ConnectionString = Servers.SqlServerConnectionString});

            loader.As<IEnvelopeStorageAdmin>().ClearAllPersistedEnvelopes();


        }

        [Fact]
        public void retrieve_creation_script()
        {
            var sql = SqlServerEnvelopeStorageAdmin.ToCreationScript("foo");

            sql.ShouldContain("create table foo.jasper_outgoing_envelopes");
            sql.ShouldContain("create table foo.jasper_incoming_envelopes");
        }

        [Fact]
        public void retrieve_drop_script()
        {
            var sql = SqlServerEnvelopeStorageAdmin.ToDropScript("foo");

            sql.ShouldContain("drop table foo.jasper_outgoing_envelopes");
            sql.ShouldContain("drop table foo.jasper_incoming_envelopes");
        }
    }
}
