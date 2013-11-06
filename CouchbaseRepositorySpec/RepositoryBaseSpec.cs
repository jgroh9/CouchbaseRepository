using System;
using System.Collections.Generic;
using Couchbase;
using CouchbaseRepositorySpec.repositories;
using CouchbaseRepositorySpec.sample_model;
using NUnit.Framework;
using Utilities;

namespace CouchbaseRepositorySpec
{
    [TestFixture]
    public class CarrierAccountTests
    {
        /// <summary>
        /// Gets or sets the couch client.
        /// </summary>
        /// <value>The couch client.</value>
        private CouchbaseClient CouchClient { get; set; }

        /// <summary>
        /// This method will run once before all tests start
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            CouchClient = new CouchbaseClient();
        }

        /// <summary>
        /// This method will run once after all tests end
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureCleanup()
        {
            CouchClient.Dispose();
        }

        [Test]
        public void CreateDocument()
        {
            const string sampleKey = "sample::1234";
            var repo = new SampleModelRepository();
            var model = new SampleModel
                {
                    Key = sampleKey,
                    SampleProperty = 99
                };
            var savedModel = repo.CreateDocument(model);

            Assert.AreEqual(model.SampleProperty, savedModel.SampleProperty);
            // validate that the created at property was set since we used the create document method
            Assert.IsNotNull(savedModel.CreatedAt);

            // cleanup after ourselves
            repo.RemoveDocument(sampleKey);
        }

        [Test]
        public void SaveDocument()
        {
            const string sampleKey = "sample::9876";
            var currentDate = DateTime.Now;

            var repo = new SampleModelRepository();
            var model = new SampleModel
                {
                    Key = sampleKey,
                    SampleProperty = 10,
                    CreatedAt = currentDate
                };
            var savedModel = repo.SaveDocument(model);

            Assert.AreEqual(model.SampleProperty, savedModel.SampleProperty);
            Assert.IsNotNull(savedModel.CreatedAt);
            // validate that the created at property was not set by the repository since we used the save document method 
            Assert.AreEqual(currentDate, savedModel.CreatedAt);

            // cleanup after ourselves
            repo.RemoveDocument(sampleKey);
        }

        [Test]
        public void UpdateDocument()
        {
            const string sampleKey = "sample::4567";
            var currentDate = DateTime.Now;
            const int updateValue = 234;

            var repo = new SampleModelRepository();
            var model = new SampleModel
                {
                    Key = sampleKey,
                    SampleProperty = 9,
                    CreatedAt = currentDate
                };
            var savedModel = repo.SaveDocument(model);

            savedModel.SampleProperty = updateValue;

            var updatedModel = repo.UpdateDocument(savedModel);

            // ensure the model was updated
            Assert.AreEqual(updateValue, updatedModel.SampleProperty);

            // cleanup after ourselves
            repo.RemoveDocument(sampleKey);
        }

        [Test]
        public void UpdateWithRetry()
        {
            const string sampleKey = "sample::0000000000";
            var repo = new SampleModelRepository();
            repo.RemoveDocument(sampleKey);

            var model0 = new SampleModel
                {
                    Key = sampleKey,
                    SampleProperty = 1,
                };
            model0 = repo.CreateDocument(model0);

            var model1 = repo.GetDocument(sampleKey);
            var model2 = repo.GetDocument(sampleKey);

            model1.SampleProperty = model0.SampleProperty + 1;
            model1 = repo.SaveDocument(model1);

            var count = 0;
            model2 = repo.UpdateDocumentWithRetry(model2, model =>
                {
                    count += 1;
                    if (count == 1)
                    {
                        model1.SampleProperty = model1.SampleProperty + 1;
                        repo.SaveDocument(model1);
                    }

                    model.SampleProperty = model.SampleProperty + 1;
                });

            Assert.AreEqual(2, count);

            var model3 = repo.GetDocument(sampleKey);
            Assert.AreEqual(3, model1.SampleProperty);
            Assert.AreEqual(4, model2.SampleProperty);
            Assert.AreEqual(4, model3.SampleProperty);

            repo.RemoveDocument(sampleKey);
        }

        [Test]
        public void UpdateWithoutRetry()
        {
            const string sampleKey = "sample::0000000000";
            var repo = new SampleModelRepository();
            repo.RemoveDocument(sampleKey);

            var model0 = new SampleModel
                {
                    Key = sampleKey,
                    SampleProperty = 1
                };
            repo.CreateDocument(model0);

            var model1 = repo.GetDocument(sampleKey);
            var model2 = repo.GetDocument(sampleKey);

            model1.SampleProperty = model0.SampleProperty + 2;
            model1 = repo.SaveDocument(model1);

            model2.SampleProperty = model0.SampleProperty + 1;
            var result = repo.UpdateDocument(model2);

            Assert.AreEqual(3, model1.SampleProperty);
            Assert.AreEqual(2, result.SampleProperty);
            Assert.AreNotEqual(model1.CasValue, result.CasValue);

            repo.RemoveDocument(sampleKey);
        }

        [Test]
        public void GetDocument()
        {
            const string sampleKey = "sample::9999";
            // setup a sample document to retrieve for the get document test
            var repo = new SampleModelRepository();
            var model = new SampleModel
                {
                    Key = sampleKey,
                    SampleProperty = 99
                };
            repo.SaveDocument(model);

            var retrievedDocument = repo.GetDocument(sampleKey);

            Assert.AreEqual(sampleKey, retrievedDocument.Key);
            Assert.AreEqual(model.SampleProperty, retrievedDocument.SampleProperty);

            // cleanup after our test
            repo.RemoveDocument(sampleKey);
        }

        [Test]
        public void IncrementDocument()
        {
            const string key = "sample::counter";
            var repo = new SampleModelRepository();
            var counterValue = repo.Increment(key, 1);
            // since the first increment is to setup the document the counter value should be 0
            Assert.AreEqual(0, counterValue);

            counterValue = repo.Increment(key, 1);

            Assert.AreEqual(1, counterValue);

            repo.RemoveDocument(key);
        }

        [Test]
        public void DecrementDocument()
        {
            const string key = "sample::counter";
            var repo = new SampleModelRepository();
            // increment the counter to the value of 2
            repo.Increment(key, 1);
            repo.Increment(key, 1);
            var counterValue = repo.Increment(key, 1);

            Assert.AreEqual(2, counterValue);

            // remove 1 from the counter
            counterValue = repo.Decrement(key, 1);

            Assert.AreEqual(1, counterValue);

            repo.RemoveDocument(key);
        }

        [Test]
        public void GetMultipleDocuments()
        {
            const string model1Key = "sample::1234";
            const string model2Key = "sample::5678";
            const string model3Key = "sample::9876";
            var repo = new SampleModelRepository();
            // create the sample list document
            var model = new SampleModel
                {
                    Key = model1Key,
                    SampleProperty = 1
                };
            var model1 = new SampleModel
                {
                    Key = model2Key,
                    SampleProperty = 2
                };
            var model2 = new SampleModel
                {
                    Key = model3Key,
                    SampleProperty = 3
                };
            repo.SaveDocument(model);
            repo.SaveDocument(model1);
            repo.SaveDocument(model2);

            var keysToGet = new List<string> {model1Key, model2Key, model3Key};

            var dictionaryOfDocs = repo.GetMultipleDocuments(keysToGet);

            Assert.NotNull(dictionaryOfDocs);
            Assert.Greater(dictionaryOfDocs.Count, 0);

            var dictionaryModel1 = dictionaryOfDocs[model1Key].ToString().FromJson<SampleModel>();
            var dictionaryModel2 = dictionaryOfDocs[model2Key].ToString().FromJson<SampleModel>();
            var dictionaryModel3 = dictionaryOfDocs[model3Key].ToString().FromJson<SampleModel>();
            Assert.AreEqual(model.SampleProperty, dictionaryModel1.SampleProperty);
            Assert.AreEqual(model1.SampleProperty, dictionaryModel2.SampleProperty);
            Assert.AreEqual(model2.SampleProperty, dictionaryModel3.SampleProperty);

            repo.RemoveDocument(model1Key);
            repo.RemoveDocument(model2Key);
            repo.RemoveDocument(model3Key);
        }
    }
}
