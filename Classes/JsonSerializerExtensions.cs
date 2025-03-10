using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BlocklistManager.Classes
{
    public static partial class JsonSerializerExtensions
    {
        [RequiresUnreferencedCode( )]
        [RequiresDynamicCode( )]
        public static T? DeserializeAnonymousType<T>( string json, T anonymousTypeObject, JsonSerializerOptions? options = default )
        {
            return JsonSerializer.Deserialize<T>( json, options );
        }

        [RequiresUnreferencedCode( )]
        [RequiresDynamicCode( )]
        public static ValueTask<TValue?> DeserializeAnonymousTypeAsync<TValue>( Stream stream, TValue anonymousTypeObject, JsonSerializerOptions? options = default, CancellationToken cancellationToken = default )
        {
            return JsonSerializer.DeserializeAsync<TValue>( stream, options, cancellationToken ); // Method to deserialize from a stream added for completeness
        }
    }
}
