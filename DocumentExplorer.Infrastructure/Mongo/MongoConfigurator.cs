﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace DocumentExplorer.Infrastructure.Mongo
{
    public static class MongoConfigurator
    {
        private static bool _initialized;
        public static void Initialize()
        {
            if(_initialized)
            {
                return;
            }
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
            RegisterConventions();
            _initialized = true;
        }

        private static void RegisterConventions()
        {
            ConventionRegistry.Register("DocumentExplorerConventions", new MongoConventions(), x => true);
        }

        private class MongoConventions : IConventionPack
        {
            public IEnumerable<IConvention> Conventions => new List<IConvention>()
            {
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String),
                new CamelCaseElementNameConvention()

            };
        }
    }
}
