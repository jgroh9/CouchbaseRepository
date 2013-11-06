using System;
using Inflector;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Utilities
{
    public static class JsonUtility
    {
        #region Declarations

        /// <summary>
        /// The serializer settings
        /// </summary>
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
                {
                    ConstructorHandling = ConstructorHandling.Default,
                    ContractResolver = new ModelResolver(),
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts objects to json.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>Json serialized object</returns>
        /// <remarks>Uses Json.Net for conversion.</remarks>
        public static string ToJson(this object source)
        {
            AddConverters();
            return JsonConvert.SerializeObject(source, SerializerSettings);
        }

        /// <summary>
        /// Converts objects from json.
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="json">The json.</param>
        /// <returns>Deserialized object of specified type.</returns>
        /// <remarks>Uses Json.Net for conversion.</remarks>
        public static T FromJson<T>(this string json)
        {
            AddConverters();
            return JsonConvert.DeserializeObject<T>(json, SerializerSettings);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds the converters.
        /// </summary>
        private static void AddConverters()
        {
            SerializerSettings.Converters.Add(new UtcDateTimeConverter());
        }

        #endregion
    }

    public class ModelResolver : DefaultContractResolver
    {
        /// <summary>
        /// Resolves the name of the property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Name of the property.</returns>
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.Underscore().ToLowerInvariant();
        }
    }

    public class UtcDateTimeConverter : DateTimeConverterBase
    {
        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null || string.IsNullOrWhiteSpace(reader.Value.ToString()))
                return null;

            return DateTime.Parse(reader.Value.ToString());
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((DateTime)value).FormatTimeToUtcString());
        }
    }
}
