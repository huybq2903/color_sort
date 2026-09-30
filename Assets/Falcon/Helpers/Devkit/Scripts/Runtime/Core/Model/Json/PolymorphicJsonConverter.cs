/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-30
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Assuming these namespaces are available from your project
// using Falcon.Modules.Core.BigData.Devkit.Scripts.Runtime.Services.Extensions; // For JsonToObj if it exists and needs settings
// using Falcon.Modules.Core.BigData.Singleton.Scripts.Runtime.Model.Interfaces; // For IInit
// using Falcon.Modules.Core.BigData.SingletonExtension.Scripts.Runtime.Model; // For SealedBox

// For Debug.LogError, common in Unity projects

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    /// <summary>
    /// A custom <see cref="JsonConverter"/> that automatically detects and instantiates
    /// a concrete type for an interface or abstract class during deserialization.
    /// <para>
    /// This converter requires that there is exactly one concrete implementation
    /// of the target interface/abstract class available in the loaded assemblies.
    /// If zero or multiple implementations are found, it will behave as follows:
    /// </para>
    /// <list type="bullet">
    ///     <item><description>Zero implementations: The converter will not be used (<see cref="CanConvert"/> returns false).</description></item>
    ///     <item><description>Multiple implementations: A <see cref="InvalidOperationException"/> will be thrown.</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// This converter is intended for scenarios where a single concrete implementation
    /// is implicitly expected for a given interface or abstract type.
    /// For complex polymorphic scenarios with multiple valid concrete types,
    /// consider using a type discriminator field in your JSON along with a more specialized converter.
    /// </remarks>
    public class PolymorphicJsonConverter : JsonConverter
    {
        /// <summary>
        /// Gets a singleton instance of the <see cref="PolymorphicJsonConverter"/>.
        /// </summary>
        public static readonly PolymorphicJsonConverter Instance = new();

        // Caches the resolved concrete type for each interface/abstract type.
        // Key: The interface or abstract base Type.
        // Value: A SealedBox containing the single resolved concrete Type, or an empty SealedBox if none.
        private readonly ConcurrentDictionary<Type, SealedBox<Type>> virtualToConcrete = new();

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The object to write.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <remarks>
        /// This converter currently delegates serialization to the default serializer.
        /// It does not automatically add type discriminators (e.g., "$type") to the JSON.
        /// </remarks>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // For write operations, we typically just let the default serializer handle the concrete object.
            // If you need to add type discriminators during serialization, you would implement that logic here,
            // potentially requiring a reverse lookup from concrete type to base type/discriminator.
            serializer.Serialize(writer, value);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object (usually an interface or abstract class).</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        /// <exception cref="JsonSerializationException">Thrown if the concrete type cannot be determined
        /// (e.g., due to an internal error or no concrete implementation found when expected).</exception>
        /// <exception cref="InvalidOperationException">Thrown if multiple concrete implementations are found
        /// for the target interface/abstract class.</exception>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // If the JSON token is null, return null directly.
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            // Load the entire JSON object from the reader.
            JObject jsonObject = JObject.Load(reader);

            // Get the resolved concrete type from the cache (or compute it if not already cached).
            SealedBox<Type> concreteTypeBox = GetConcreteImplementations(objectType);

            // If no concrete type could be resolved (e.g., no implementations found),
            // this indicates an unexpected state, as CanConvert should have prevented this.
            if (!concreteTypeBox.HasValue)
            {
                throw new JsonSerializationException($"Could not determine concrete type for interface/abstract '{objectType.Name}'. " +
                                                     "This might indicate that no suitable concrete implementation was found " +
                                                     "or that an unexpected state occurred after CanConvert returned true.");
            }

            // Deserialize the loaded JObject into the determined concrete type.
            return jsonObject.ToObject(concreteTypeBox.Value, serializer);
        }

        /// <summary>
        /// Determines whether this converter can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type to check for convertibility.</param>
        /// <returns>
        /// <see langword="true"/> if the <paramref name="objectType"/> is an interface or an abstract class
        /// and a single concrete implementation can be resolved for it; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This converter specifically handles interfaces and abstract classes. For concrete types,
        /// it explicitly returns <see langword="false"/> to allow other converters or the default
        /// Json.NET deserializer to process them.
        /// An <see cref="InvalidOperationException"/> may be thrown by <see cref="GetConcreteImplementations"/>
        /// if multiple concrete implementations are found for the target type.
        /// </remarks>
        public override bool CanConvert(Type objectType)
        {
            // This converter only handles interfaces and abstract classes.
            // For concrete classes, we explicitly return false, letting other converters or
            // the default Json.NET serializer handle them.
            if (!objectType.IsInterface && (!objectType.IsClass || !objectType.IsAbstract))
            {
                return false;
            }

            // Attempt to get the concrete implementation.
            // If GetConcreteImplementations throws due to multiple matches, CanConvert will propagate it.
            // If it returns an empty box (no match), CanConvert returns false.
            // If it returns a value, CanConvert returns true.
            var sealedBox = GetConcreteImplementations(objectType);
            return sealedBox.HasValue;
        }

        /// <summary>
        /// Resolves and caches the single concrete implementation <see cref="Type"/> for a given
        /// interface or abstract class <paramref name="objectType"/>.
        /// </summary>
        /// <param name="objectType">The interface or abstract class <see cref="Type"/> to find the concrete implementation for.</param>
        /// <returns>
        /// A <see cref="SealedBox{T}"/> containing the single concrete <see cref="Type"/> found,
        /// or an empty <see cref="SealedBox{T}"/> if no implementations are found.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if more than one concrete implementation
        /// is found for the specified <paramref name="objectType"/>, as the converter cannot
        /// automatically determine which one to use.</exception>
        private SealedBox<Type> GetConcreteImplementations(Type objectType)
        {
            // Use ConcurrentDictionary.GetOrAdd for thread-safe caching and lookup.
            // The valueFactory is executed only if the key is not already present.
            return virtualToConcrete.GetOrAdd(objectType, (keyType) =>
            {
                var concreteImplementations = FindConcreteImplementations(keyType);

                return concreteImplementations.Count switch
                {
                    0 =>
                        // No concrete implementations found for this interface/abstract type.
                        SealedBox.Empty<Type>(),
                    1 =>
                        // Exactly one concrete implementation found, return it.
                        SealedBox.Of(concreteImplementations[0]),
                    _ => throw new InvalidOperationException(
                        $"Too many concrete implementations found for '{keyType.Name}': " +
                        $"{string.Join(", ", concreteImplementations.Select(type => type.Name))}. " +
                        "This converter requires exactly one concrete implementation for automatic detection. " +
                        "Consider using a type discriminator in your JSON or a more specialized converter for this type.")
                };
            });
        }

        /// <summary>
        /// Finds all concrete, non-abstract, non-interface types within loaded assemblies
        /// that implement a specified interface or inherit from a specified abstract class.
        /// </summary>
        /// <param name="baseType">The interface or abstract class <see cref="Type"/> to find implementations/derivations for.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="Type"/> representing the found concrete types.</returns>
        private static List<Type> FindConcreteImplementations(Type baseType)
        {

            List<Type> concreteImplementations = new List<Type>();
            // Filter types: Must be a class, not abstract, not an interface, not an open generic type definition,
            // and assignable from the baseType.
            var validTypes = FReflection.FReflection.Instance.GetTypes()
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    !type.IsInterface &&
                    !type.IsGenericTypeDefinition && // Exclude List<>, Dictionary<,> etc.
                    baseType.IsAssignableFrom(type)); // Checks if type implements/inherits baseType

            concreteImplementations.AddRange(validTypes);

            return concreteImplementations;
        }
    }
}