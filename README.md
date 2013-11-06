CouchbaseRepository
===================

This is a basic Couchbase repository that can be used to get started building out C# applications with Couchbase. Feel free to fork the code and customize it to your own needs.

A few things to note
- You will need to have Couchbase Server installed in order to run the unit tests. You can set the parameters to your Couchbase installation in the App.config file of the CouchbaseRepository project as well as within the unit test project
- I've used NuGet to get a few necessary libraries to help out with the Repository. Obviously the Couchbase C# client is needed but I've also used JSON.NET and Inflector. 
- Within the RepositoryBase class there is a LogCouchbaseOperationResult method that can be used to log issues to whatever logging solution you chose. For simplicity sake, I've just chosen to log to the output window within Visual Studio. You will probably want to use a solution like NLog which can be used to log to text files or services like PaperTrail.

