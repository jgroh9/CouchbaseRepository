using System;
using Newtonsoft.Json;

namespace CouchbaseRepository
{
    [Serializable]
    public abstract class ModelBase
    {
        #region Declarations

        public const string KeySeparator = "::";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the object key.
        /// </summary>
        /// <value>The key.</value>
        [JsonProperty(PropertyName = "id")]
        public string Key { get; set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>The type.</value>
        [JsonProperty("type")]
        public abstract string Type { get; }

        /// <summary>
        /// Gets or sets the create date.
        /// </summary>
        /// <value>The create date.</value>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated attribute.
        /// </summary>
        /// <value>The updated attribute.</value>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the cas value.
        /// </summary>
        /// <value>The cas value.</value>
        [JsonIgnore]
        public ulong CasValue { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public long Version { get; set; }

        #endregion
    }
}
