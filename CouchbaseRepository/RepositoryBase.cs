#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Couchbase;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;
using Inflector;
using Utilities;
#endregion

namespace CouchbaseRepository
{
    public class RepositoryBase<T> where T : ModelBase
    {
        #region Declarations

        protected CouchbaseClient Client = null;
        private const int DefaultCounterValue = 0;
        private readonly string _className = string.Empty;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes static members of the <see cref="RepositoryBase{T}" /> class.
        /// </summary>
        public RepositoryBase()
        {
            _className = GetType().Namespace + "::" + GetType().Name;

            if (Client == null)
                Client = CouchbaseManager.CouchbaseClient;
        }

        #endregion

        #region Interface Methods

        /// <summary>
        /// Creates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>the latest version of the document</returns>
        public virtual T CreateDocument(T model)
        {
            model.CreatedAt = DateTime.UtcNow;
            return StoreDocument(StoreMode.Add, model);
        }

        /// <summary>
        /// Updates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>the latest version of the document</returns>
        public virtual T UpdateDocument(T model)
        {
            return StoreDocument(StoreMode.Replace, model);
        }

        /// <summary>
        /// Loads a clean copy of the model and passes it to the given
        /// block, which should apply changes to that model that can
        /// be retried from scratch multiple times until successful.
        /// 
        /// This method will return the final state of the saved model.
        /// The caller should use this afterward, instead of the instance
        /// it had passed in to the call.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="block">The block.</param>
        /// <returns>the latest version of the model provided to the method</returns>
        public virtual T UpdateDocumentWithRetry(T model, Action<T> block)
        {
            var documentKey = model.Key;
            var success = false;
            T documentToReturn = null;

            while (!success)
            {
                // load a clean copy of the document 
                var latestDocument = GetDocument(documentKey);
                // if we were unable to find a document then create the document and 
                // return the latest version of the document to the caller
                if (latestDocument == null)
                    return CreateDocument(model);

                // pass the latest document to the given block so the latest changes can be applied
                if (block != null)
                    block(latestDocument);

                latestDocument.Version += 1;
                latestDocument.UpdatedAt = SetUpdatedAtWithNoTimeTravel(model.UpdatedAt);
                var latestDocumentJson = latestDocument.ToJson();
                var storeResult = Client.ExecuteCas(StoreMode.Replace, latestDocument.Key, latestDocumentJson,
                                                    latestDocument.CasValue);
                if (!storeResult.Success) 
                    continue;

                documentToReturn = latestDocument;
                success = true;
            }

            return documentToReturn;
        }

        /// <summary>
        /// Saves the specified model. If a document exists then it is 
        /// updated, if the document doesn't exist then it will be created.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>the latest version of the document</returns>
        public virtual T SaveDocument(T model)
        {
            return StoreDocument(StoreMode.Set, model);
        }

        /// <summary>
        /// Gets the document.
        /// </summary>
        /// <param name="documentKey">The document key.</param>
        /// <param name="isRetry">if set to <c>true</c> [is retry].</param>
        /// <returns>the latest version of a document with type T</returns>
        public virtual T GetDocument(string documentKey, bool isRetry = false)
        {
            // make sure the document key is actually set
            if (string.IsNullOrEmpty(documentKey))
                return null;

            var getResult = Client.GetWithCas<string>(documentKey);
            if (!string.IsNullOrWhiteSpace(getResult.Result))
            {
                var doc = getResult.Result.FromJson<T>();
                // server doesn't pass back the _id in the JSON
                doc.Key = documentKey; 
                // set the cas value so we can use it when we try to save the document back to couchbase
                doc.CasValue = getResult.Cas;
                return doc;
            }

            // check to be sure the document exists. if it does then try to loop a maximum of 9 times
            // before giving up. It's been found that sometimes the document exists but for whatever 
            // reason it is not returned. 
            if (Client.KeyExists(documentKey))
            {
                int i = 0;
                while (Client.KeyExists(documentKey) && i < 9)
                {
                    var result = Client.GetWithCas<string>(documentKey);
                    if (!string.IsNullOrWhiteSpace(result.Result))
                    {
                        var doc = result.Result.FromJson<T>();
                        // server doesn't pass back the _id in the JSON
                        doc.Key = documentKey; 
                        // set the cas value so we can use it when we try to save the document back to couchbase
                        doc.CasValue = result.Cas;
                        return doc;
                    }
                    i++;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a list of documents.
        /// </summary>
        /// <param name="documentKey">The document key.</param>
        /// <param name="isRetry">if set to <c>true</c> [is retry].</param>
        /// <returns>List{`0}.</returns>
        public virtual List<T> GetListDocument(string documentKey, bool isRetry = false)
        {
            // make sure the document key is actually set
            if (string.IsNullOrEmpty(documentKey))
                return null;

            var getResult = Client.ExecuteGet<string>(documentKey);
            // make sure the get operation succeeded
            if (getResult.Success && getResult.HasValue)
            {
                var result = getResult.Value;
                var docList = result.FromJson<List<T>>();
                return docList;
            }

            if (!isRetry)
            {
                LogCouchbaseOperationResult(documentKey, ToString(), "Failed to Get a list of documents", getResult);
                GetListDocument(documentKey, true);
            }

            return null;
        }

        /// <summary>
        /// Removes the document.
        /// </summary>
        /// <param name="documentKey">The document key.</param>
        /// <param name="casValue">The cas value.</param>
        /// <returns><c>true</c> if the document was successfully removed, <c>false</c> otherwise</returns>
        public virtual bool RemoveDocument(string documentKey, ulong casValue = 0)
        {
            // loop until we have validated that the document has been 
            // deleted or until we have looped 10 times
            int i = 0;
            while (Client.KeyExists(documentKey) && i < 9)
            {
                i++;

                if (casValue == 0)
                {
                    Client.Remove(documentKey);
                    continue;
                }
              
                Client.Remove(documentKey, casValue);
            }

            return (i < 9);
        }

        /// <summary>
        /// Increments the specified document key.
        /// </summary>
        /// <param name="documentKey">The document key.</param>
        /// <param name="incrementValue">The increment value.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>System.Int64.</returns>
        public virtual long Increment(string documentKey, int incrementValue, ulong defaultValue = DefaultCounterValue)
        {
            var result = Client.ExecuteIncrement(documentKey, defaultValue, (ulong)incrementValue);
            if (!result.Success)
            {
                LogCouchbaseOperationResult(documentKey, "Increment", "Increment failed", result);
            }

            return (long) result.Value;
        }

        /// <summary>
        /// Decrements the specified document key.
        /// </summary>
        /// <param name="documentKey">The document key.</param>
        /// <param name="decrementValue">The decrement value.</param>
        /// <returns>System.Int64.</returns>
        public virtual long Decrement(string documentKey, int decrementValue)
        {
            return (long)Client.Decrement(documentKey, DefaultCounterValue, (ulong)decrementValue);
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Given an array of keys, gets all the objects associated with those keys in a single call.
        /// This method is more efficient then making multiple calls for a group of documents
        /// </summary>
        /// <param name="keysToGet">The keys to get.</param>
        /// <returns>Dictionary{System.StringSystem.Object}.</returns>
        public virtual IDictionary<string, object> GetMultipleDocuments(IEnumerable<string> keysToGet)
        {
            IDictionary<string, object> docs = Client.Get(keysToGet);
            return docs ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Get's data from the specified view
        /// </summary>
        /// <param name="viewName">Name of the view.</param>
        /// <returns>IView{IViewRow}.</returns>
        /// <remarks>
        /// This method assumes document design name has been named just like it's 
        /// associated model but pluralized.
        /// </remarks>
        protected virtual IView<IViewRow> View(string viewName)
        {
            return Client.GetView(typeof(T).Name.ToLower().Pluralize(), viewName);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Stores the document.
        /// </summary>
        /// <param name="mode">The storage mode.</param>
        /// <param name="model">The model.</param>
        /// <returns><c>true</c> if the document was successfully stored, <c>false</c> otherwise</returns>
        private T StoreDocument(StoreMode mode, T model)
        {
            model.Version += 1;
            model.UpdatedAt = SetUpdatedAtWithNoTimeTravel(model.UpdatedAt);

            var json = model.ToJson();
            var docKey = model.Key;

            var result = Client.ExecuteStore(mode, docKey, json);
            if (result.Success)
            {
                // set the cas value so we can use it later on
                model.CasValue = result.Cas;
                return model;
            }

            int i = 0;
            while (i < 9)
            {
                var storeResult = Client.ExecuteStore(mode, docKey, json);
                if (storeResult.Success)
                {
                    // set the cas value so we can use it later on
                    model.CasValue = storeResult.Cas;
                    return model; 
                }

                i++;
            }

            string message = "Store failed for the key " + docKey + ", after trying 10 times to store the document.";
            LogCouchbaseOperationResult(docKey, ToString(), message, result);

            return model;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Determine what the updated at date and time should be while ensuring that the current updated at date and time 
        /// is not greater then the current date and time. this sometimes happens due to clock drift between the linux 
        /// and windows servers due to the windows servers not keeping the proper time. To ensure we don't send states
        /// back to the front end users in an out of order situation we must correct any clock drift.
        /// </summary>
        /// <param name="currentUpdatedAtDateTime">The current updated at date time.</param>
        /// <returns>DateTime.</returns>
        private static DateTime SetUpdatedAtWithNoTimeTravel(DateTime currentUpdatedAtDateTime)
        {
            // Ensure that there is no time traveling.
            var now = DateTime.UtcNow;

            // due to a difference in timing between linux boxes and windows boxes we need to be sure
            // that the last modified date that is currently set in the carrier account document
            // is less then the current windows time.
            if (currentUpdatedAtDateTime > now)
            {
                now = currentUpdatedAtDateTime.AddMilliseconds(1);
            }

            return now;
        }

        /// <summary>
        /// Logs the couchbase operation result.
        /// </summary>
        /// <param name="documentKey">The document key.</param>
        /// <param name="method">The method.</param>
        /// <param name="message">The message.</param>
        /// <param name="result">The result.</param>
        private void LogCouchbaseOperationResult(string documentKey, string method, string message, IOperationResult result)
        {
            var statusCode = result.StatusCode ?? 0;
            var resultMessage = (!string.IsNullOrWhiteSpace(result.Message)) ? result.Message : string.Empty;
            var exception = result.Exception;
            var exceptionMessage = string.Empty;
            if (exception != null)
            {
                exceptionMessage = exception.Message;
            }

            var innerResult = (result.InnerResult != null) ? result.InnerResult.Message : string.Empty;

            var messageToLog = string.Format("{0}::{1} | {2} with a key of {3}. Here are the IOperationResults\n"
                                        + "StatusCode = {4}\nMessage = {5}\nExceptionMessage = {6}\nInnerResult = {7}",
                                        _className, method, message, documentKey, statusCode, resultMessage,
                                        exceptionMessage, innerResult);
            Debug.WriteLine(messageToLog);
        }

        #endregion
    }
}
