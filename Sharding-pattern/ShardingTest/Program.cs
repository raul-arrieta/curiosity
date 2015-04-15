using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ShardingTest
{
    /// <summary>
    /// A Shard
    /// </summary>
    class Shard {
        /// <summary>
        /// Connection string for this Shard
        /// </summary>
        public string ConnectionString {get;set;}

        /// <summary>
        /// Shard Id
        /// </summary>
        public int Id {get; set;}

        /// <summary>
        /// Shard Constructor
        /// </summary>
        /// <param name="connectionString">Connection string for this Shard</param>
        /// <param name="id">Shard Id</param>
        public Shard (string connectionString, int id){
            this.ConnectionString = connectionString;
            this.Id = id;
        }
    }

    /// <summary>
    /// Simulate sharding against same database.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Query to execute
        /// </summary>
        const string QUERY = "select value from test_sharding";

        /// <summary>
        /// Number of shards to run query
        /// </summary>
        const int NUM_SHARDS = 20;

        static void Main(string[] args)
        {
            var watch = Stopwatch.StartNew(); ;

            #region RunSequential
            Console.WriteLine("RunSequential:");
            watch = Stopwatch.StartNew();
            RunSequential();
            watch.Stop();
            Console.WriteLine("Elapsed milliseconds: {0}", watch.ElapsedMilliseconds); 
            #endregion
            
            Console.WriteLine("");

            #region TestParalell
            Console.WriteLine("TestParalell: ");
            watch = Stopwatch.StartNew();
            RunShardingParallel();
            watch.Stop();
            Console.WriteLine("Elapsed milliseconds: {0}", watch.ElapsedMilliseconds); 
            #endregion
            
            Console.WriteLine("");

            #region TestAsync
            Console.WriteLine("TestAsync: ");
            watch = Stopwatch.StartNew();
            RunShardingParallel();
            watch.Stop();
            Console.WriteLine("Elapsed milliseconds: {0}", watch.ElapsedMilliseconds); 
            #endregion
            
        }

        /// <summary>
        /// Run queries sequentally
        /// </summary>
        static void RunSequential()
        {
            List<Shard> shards = GetShards();

            var results = new List<string>();

            foreach(Shard shard in shards)
            {
                // NOTE: Transient fault handling is not included, 
                // but should be incorporated when used in a real world application.
                using (var con = new MySqlConnection(shard.ConnectionString))
                {
                    con.Open();
                    var cmd = new MySqlCommand(QUERY, con);

                    Console.WriteLine("BEGIN > Executing command against shard: {0}", shard.Id);
                    var internal_watch = Stopwatch.StartNew();
                    var reader = cmd.ExecuteReader();
                    // Read the results in to a thread-safe data structure.
                    while (reader.Read())
                    {
                        results.Add(reader.GetString(0));
                    }
                    internal_watch.Stop();
                    Console.WriteLine("END > Executing command against shard: {0} - {1}", shard.Id, internal_watch.ElapsedMilliseconds);
                }
            }
            Console.WriteLine("Fanout query complete - Record Count: {0}", results.Count);
        }
        
        /// <summary>
        /// Shardding Pattern Standar (Parallel version)
        /// https://msdn.microsoft.com/es-es/library/dn589797.aspx
        /// </summary>
        static void RunShardingParallel()
        {
          // Retrieve the shards as a ShardInformation[] instance. 
          List<Shard> shards = GetShards();

          var results = new ConcurrentBag<string>();

          // Execute the query against each shard in the shard list.
          // This list would typically be retrieved from configuration 
          // or from a root/master shard store.
          Parallel.ForEach(shards, shard =>
          {
            // NOTE: Transient fault handling is not included, 
            // but should be incorporated when used in a real world application.
            using (var con = new MySqlConnection(shard.ConnectionString))
            {
              con.Open();
              var cmd = new MySqlCommand(QUERY, con);

              Console.WriteLine("BEGIN > Executing command against shard: {0}", shard.Id);
              var internal_watch = Stopwatch.StartNew();
              var reader = cmd.ExecuteReader();
              // Read the results in to a thread-safe data structure.
              while (reader.Read())
              {
                results.Add(reader.GetString(0));
              }

              internal_watch.Stop();
              Console.WriteLine("END > Executing command against shard: {0} - {1}", shard.Id, internal_watch.ElapsedMilliseconds);
            }
          });

          Console.WriteLine("Fanout query complete - Record Count: {0}", results.Count);
        }

        /// <summary>
        /// Shardding Pattern async await person
        /// </summary>
        static async void RunShardingAsyncAwait() {
          // Retrieve the shards as a ShardInformation[] instance. 
          List<Shard> shards = GetShards();

          var results = new ConcurrentBag<string>();

          var tasks = shards.Select(async shard =>
          {
            using (var con = new MySqlConnection(shard.ConnectionString))
            {
              con.Open();
              var cmd = new MySqlCommand(QUERY, con);

              Console.WriteLine("BEGIN > Executing command against shard: {0}", shard.Id);
              var internal_watch = Stopwatch.StartNew();
              var reader = cmd.ExecuteReader();
              // Read the results in to a thread-safe data structure.
              while (reader.Read())
              {
                results.Add(reader.GetString(0));
              }
              internal_watch.Stop();
              Console.WriteLine("END > Executing command against shard: {0} - {1}", shard.Id, internal_watch.ElapsedMilliseconds);
            }
          });
          await Task.WhenAll(tasks);

          Console.WriteLine("Fanout query complete - Record Count: {0}", 
                                  results.Count);
        }

        /// <summary>
        /// Generate NUM_SHARDS Shards
        /// </summary>
        /// <returns>List of NUM_SHARD Shards</returns>
        static List<Shard> GetShards()
        {
            List<Shard> shards = new List<Shard>();
            for (var i = 0; i < NUM_SHARDS; i++)
            {
                shards.Add(new Shard(GetConnectionString(), i));
            }
            return shards;
        }

        /// <summary>
        /// Generate connection string
        /// For db4free.net
        /// http://www.db4free.net provides a testing service for the latest version of the MySQL Server.
        /// </summary>
        /// <returns>connection string</returns>
        static string GetConnectionString()
        {
            const string host = "db4free.net";
            const string port = "3306";
            const string database = "curiosityshardbd";
            const string user = "curiosityshard";
            const string password = "curiosity";

            return "server=" + host + ";port=" + port + ";database=" + database + ";User ID=" + user + ";password=" + password + ";";
        }
    }
}
