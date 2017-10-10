using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Client.Providers;

namespace TreasureHunter.DataAccess
{
    public class Accessor
    {

        public void Init()
        {
            var section = ConfigurationManager.GetSection("couchbase");
            ClusterHelper.Initialize(new ClientConfiguration((CouchbaseClientSection)section));
            var bucket = ClusterHelper.GetBucket("beer-sample");
            var result = bucket.Query<dynamic>("SELECT name FROM `beer-sample`");
        }
        
    }
}
