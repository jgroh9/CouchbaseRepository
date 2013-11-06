using Couchbase;

// <summary>
// In practice, it's expensive to create clients. The client incurs overhead as it 
// creates connection pools and sets up the thread to get cluster configuration. 
// Therefore, the best practice is to create a single client instance, per bucket, 
// per AppDomain. Creating a static property on a class works well for this purpose. 
//
// See this webpage for details: http://www.couchbase.com/docs/couchbase-sdk-net-1.2/clientinstantiation.html
// </summary>
namespace CouchbaseRepository
{
    /// <summary>
    /// Class CouchbaseManager
    /// </summary>
    public static class CouchbaseManager
    {
        #region Declarations

        private static readonly CouchbaseClient Instance;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the couchbase client.
        /// </summary>
        /// <value>The couchbase client.</value>
        public static CouchbaseClient CouchbaseClient
        {
            get { return Instance; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes static members of the <see cref="CouchbaseManager" /> class.
        /// </summary>
        static CouchbaseManager()
        {
            Instance = new CouchbaseClient();
        }

        #endregion
    }
}
