﻿using System;
using System.Linq;
using Baseline;
using Jasper.Settings;
using Microsoft.AspNetCore.Hosting;

namespace Jasper.Persistence.Postgresql
{
    public static class PostgresqlConfigurationExtensions
    {
        /// <summary>
        ///     Register sql server backed message persistence to a known connection string
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="connectionString"></param>
        /// <param name="schema"></param>
        public static void PersistMessagesWithPostgresql(this JasperSettings settings, string connectionString,
            string schema = null)
        {
            var parent = settings.As<IHasRegistryParent>().Parent;
            if (!parent.AppliedExtensions.OfType<PostgresqlBackedPersistence>().Any())
                parent.Include<PostgresqlBackedPersistence>();

            settings.Alter<PostgresqlSettings>(x =>
            {
                x.ConnectionString = connectionString;
                if (schema.IsNotEmpty()) x.SchemaName = schema;
            });
        }

        /// <summary>
        ///     Register sql server backed message persistence based on configuration and the
        ///     development environment
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="configure"></param>
        public static void PersistMessagesWithPostgresql(this JasperSettings settings,
            Action<WebHostBuilderContext, PostgresqlSettings> configure)
        {
            var parent = settings.As<IHasRegistryParent>().Parent;
            if (!parent.AppliedExtensions.OfType<PostgresqlBackedPersistence>().Any())
                parent.Include<PostgresqlBackedPersistence>();

            settings.Alter(configure);
        }
    }
}
