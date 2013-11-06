using System;
using CouchbaseRepository;

namespace CouchbaseRepositorySpec.sample_model
{
    public class SampleModel : ModelBase
    {
        private const string DocumentType = "sample";

        /// <summary>
        /// Gets or sets the sample property.
        /// </summary>
        /// <value>The sample property.</value>
        public int SampleProperty { get; set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>The type.</value>
        /// <exception cref="NotImplementedException"></exception>
        public override string Type
        {
            get { return DocumentType; }
        }
    }
}
