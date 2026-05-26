using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Application.Suppliers.Ports.Output;
using ServicioProveedores.Domain.Shared.Exceptions;
using ServicioProveedores.Domain.Suppliers.Entities;
using System.Threading;

namespace ServicioProveedores.Infrastructure.Suppliers.Persistence;

public class SupplierRepository : ISupplierRepository
{
    private const string ActiveState = "A";
    private const string InactiveState = "I";
    private const int MaximumSupplierCodeNumber = 999;

    private const string SupplierCodeSequenceId = "supplier_code";
    private const string SupplierIdSequenceId = "supplier_id";

    private readonly IMongoCollection<SupplierDocument> _suppliers;
    private readonly IMongoCollection<SequenceDocument> _sequences;
    private readonly ILogger<SupplierRepository> _logger;

    private static readonly object IndexInitializationSync = new();
    private static int _indexesReady;

    public SupplierRepository(
        IMongoClient mongoClient,
        IConfiguration configuration,
        ILogger<SupplierRepository> logger)
    {
        _logger = logger;

        var databaseName = configuration["Mongo:Database"];
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            databaseName = "mercadito_suppliers";
        }

        var suppliersCollectionName = configuration["Mongo:SuppliersCollection"];
        if (string.IsNullOrWhiteSpace(suppliersCollectionName))
        {
            suppliersCollectionName = "suppliers";
        }

        var sequencesCollectionName = configuration["Mongo:SequencesCollection"];
        if (string.IsNullOrWhiteSpace(sequencesCollectionName))
        {
            sequencesCollectionName = "sequences";
        }

        var database = mongoClient.GetDatabase(databaseName);
        _suppliers = database.GetCollection<SupplierDocument>(suppliersCollectionName);
        _sequences = database.GetCollection<SequenceDocument>(sequencesCollectionName);

        EnsureIndexesBestEffort();
    }

    public async Task<string> GetNextSupplierCodeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var nextNumber = await PeekNextSequenceValueAsync(
                SupplierCodeSequenceId,
                GetMaxSupplierCodeNumberAsync,
                cancellationToken);

            if (nextNumber > MaximumSupplierCodeNumber)
            {
                throw new BusinessValidationException("Codigo", "No hay más códigos de proveedor disponibles.");
            }

            return FormatSupplierCode((int)nextNumber);
        }
        catch (BusinessValidationException)
        {
            throw;
        }
        catch (MongoException exception)
        {
            throw CreateDataStoreUnavailableException("consultar el siguiente código de proveedor", exception);
        }
    }

    public async Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<SupplierDocument>.Filter.Eq(supplier => supplier.State, ActiveState);
            var documents = await _suppliers
                .Find(filter)
                .SortBy(supplier => supplier.RazonSocial)
                .ThenBy(supplier => supplier.Id)
                .ToListAsync(cancellationToken);

            return documents.Select(MapToDomain).ToList();
        }
        catch (MongoException exception)
        {
            throw CreateDataStoreUnavailableException("consultar los proveedores", exception);
        }
    }

    public async Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<SupplierDocument>.Filter.And(
                Builders<SupplierDocument>.Filter.Eq(supplier => supplier.Id, id),
                Builders<SupplierDocument>.Filter.Eq(supplier => supplier.State, ActiveState));

            var document = await _suppliers.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (document == null)
            {
                return null;
            }

            return MapToDomain(document);
        }
        catch (MongoException exception)
        {
            throw CreateDataStoreUnavailableException("consultar el proveedor", exception);
        }
    }

    public async Task<long> CreateAsync(CreateSupplierDto entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            var code = await ReserveNextSupplierCodeAsync(cancellationToken);
            var supplierId = await ReserveNextSequenceValueAsync(
                SupplierIdSequenceId,
                GetMaxSupplierIdAsync,
                cancellationToken);

            var now = DateTime.UtcNow;
            var document = new SupplierDocument
            {
                Id = supplierId,
                Codigo = code,
                RazonSocial = NormalizeValue(entity.Nombre),
                Direccion = NormalizeValue(entity.Direccion),
                Contacto = NormalizeValue(entity.Contacto),
                Rubro = NormalizeValue(entity.Rubro),
                Telefono = NormalizeValue(entity.Telefono),
                State = ActiveState,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _suppliers.InsertOneAsync(document, cancellationToken: cancellationToken);
            return supplierId;
        }
        catch (BusinessValidationException)
        {
            throw;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new BusinessValidationException("Codigo", "Ya existe un proveedor con ese código.");
        }
        catch (MongoException exception)
        {
            throw CreateDataStoreUnavailableException("crear el proveedor", exception);
        }
    }

    public async Task<int> UpdateAsync(UpdateSupplierDto entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            var existing = await _suppliers
                .Find(Builders<SupplierDocument>.Filter.And(
                    Builders<SupplierDocument>.Filter.Eq(supplier => supplier.Id, entity.Id),
                    Builders<SupplierDocument>.Filter.Eq(supplier => supplier.State, ActiveState)))
                .FirstOrDefaultAsync(cancellationToken);

            if (existing == null)
            {
                return 0;
            }

            var duplicateCodeFilter = Builders<SupplierDocument>.Filter.And(
                Builders<SupplierDocument>.Filter.Eq(supplier => supplier.Codigo, NormalizeCode(entity.Codigo)),
                Builders<SupplierDocument>.Filter.Ne(supplier => supplier.Id, entity.Id));

            var duplicate = await _suppliers.Find(duplicateCodeFilter).AnyAsync(cancellationToken);
            if (duplicate)
            {
                throw new BusinessValidationException("Codigo", "Ya existe un proveedor con ese código.");
            }

            var update = Builders<SupplierDocument>.Update
                .Set(supplier => supplier.Codigo, NormalizeCode(entity.Codigo))
                .Set(supplier => supplier.RazonSocial, NormalizeValue(entity.Nombre))
                .Set(supplier => supplier.Direccion, NormalizeValue(entity.Direccion))
                .Set(supplier => supplier.Contacto, NormalizeValue(entity.Contacto))
                .Set(supplier => supplier.Rubro, NormalizeValue(entity.Rubro))
                .Set(supplier => supplier.Telefono, NormalizeValue(entity.Telefono))
                .Set(supplier => supplier.UpdatedAt, DateTime.UtcNow);

            var result = await _suppliers.UpdateOneAsync(
                Builders<SupplierDocument>.Filter.Eq(supplier => supplier.Id, entity.Id),
                update,
                cancellationToken: cancellationToken);

            return (int)result.ModifiedCount;
        }
        catch (BusinessValidationException)
        {
            throw;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new BusinessValidationException("Codigo", "Ya existe un proveedor con ese código.");
        }
        catch (MongoException exception)
        {
            throw CreateDataStoreUnavailableException("actualizar el proveedor", exception);
        }
    }

    public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            var update = Builders<SupplierDocument>.Update
                .Set(supplier => supplier.State, InactiveState)
                .Set(supplier => supplier.UpdatedAt, DateTime.UtcNow);

            var filter = Builders<SupplierDocument>.Filter.And(
                Builders<SupplierDocument>.Filter.Eq(supplier => supplier.Id, id),
                Builders<SupplierDocument>.Filter.Eq(supplier => supplier.State, ActiveState));

            var result = await _suppliers.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return (int)result.ModifiedCount;
        }
        catch (MongoException exception)
        {
            throw CreateDataStoreUnavailableException("desactivar el proveedor", exception);
        }
    }

    private static string NormalizeValue(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private async Task<string> ReserveNextSupplierCodeAsync(CancellationToken cancellationToken)
    {
        var preview = await PeekNextSequenceValueAsync(
            SupplierCodeSequenceId,
            GetMaxSupplierCodeNumberAsync,
            cancellationToken);

        if (preview > MaximumSupplierCodeNumber)
        {
            throw new BusinessValidationException("Codigo", "No hay más códigos de proveedor disponibles.");
        }

        var reserved = await ReserveNextSequenceValueAsync(
            SupplierCodeSequenceId,
            GetMaxSupplierCodeNumberAsync,
            cancellationToken);

        if (reserved > MaximumSupplierCodeNumber)
        {
            throw new BusinessValidationException("Codigo", "No hay más códigos de proveedor disponibles.");
        }

        return FormatSupplierCode((int)reserved);
    }

    private static string NormalizeCode(string? value)
    {
        return NormalizeValue(value).ToUpperInvariant();
    }

    private static string FormatSupplierCode(int codeNumber)
    {
        return $"PRV{codeNumber:000}";
    }

    private static DataStoreUnavailableException CreateDataStoreUnavailableException(string operation, Exception exception)
    {
        return new DataStoreUnavailableException($"No se pudo {operation} porque la base de datos no está disponible.", exception);
    }

    private void EnsureIndexesBestEffort()
    {
        if (Volatile.Read(ref _indexesReady) == 1)
        {
            return;
        }

        lock (IndexInitializationSync)
        {
            if (Volatile.Read(ref _indexesReady) == 1)
            {
                return;
            }

            try
            {
                EnsureIndexes();
                Volatile.Write(ref _indexesReady, 1);
            }
            catch (MongoException exception)
            {
                _logger.LogWarning(
                    exception,
                    "No se pudieron asegurar los índices de proveedores. Se reintentará en la siguiente solicitud.");
            }
        }
    }

    private void EnsureIndexes()
    {
        var codeIndex = new CreateIndexModel<SupplierDocument>(
            Builders<SupplierDocument>.IndexKeys.Ascending(supplier => supplier.Codigo),
            new CreateIndexOptions { Unique = true, Name = "ux_supplier_code" });

        _suppliers.Indexes.CreateOne(codeIndex);
    }

    private async Task<long> PeekNextSequenceValueAsync(
        string sequenceId,
        Func<CancellationToken, Task<long>> fallbackNextValueFactory,
        CancellationToken cancellationToken)
    {
        var sequence = await _sequences
            .Find(Builders<SequenceDocument>.Filter.Eq(item => item.Id, sequenceId))
            .FirstOrDefaultAsync(cancellationToken);

        if (sequence != null)
        {
            return sequence.NextValue;
        }

        var fallbackNextValue = await fallbackNextValueFactory(cancellationToken);
        await InitializeSequenceIfMissingAsync(sequenceId, fallbackNextValue, cancellationToken);
        return fallbackNextValue;
    }

    private async Task<long> ReserveNextSequenceValueAsync(
        string sequenceId,
        Func<CancellationToken, Task<long>> fallbackNextValueFactory,
        CancellationToken cancellationToken)
    {
        var nextValue = await PeekNextSequenceValueAsync(sequenceId, fallbackNextValueFactory, cancellationToken);

        var filter = Builders<SequenceDocument>.Filter.Eq(item => item.Id, sequenceId);
        var update = Builders<SequenceDocument>.Update.Inc(item => item.NextValue, 1);
        var options = new FindOneAndUpdateOptions<SequenceDocument, SequenceDocument>
        {
            ReturnDocument = ReturnDocument.Before
        };

        var sequence = await _sequences.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return sequence?.NextValue ?? nextValue;
    }

    private async Task InitializeSequenceIfMissingAsync(
        string sequenceId,
        long nextValue,
        CancellationToken cancellationToken)
    {
        try
        {
            await _sequences.InsertOneAsync(new SequenceDocument
            {
                Id = sequenceId,
                NextValue = Math.Max(1, nextValue)
            }, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // another request initialized it first
        }
    }

    private async Task<long> GetMaxSupplierIdAsync(CancellationToken cancellationToken)
    {
        var lastSupplier = await _suppliers
            .Find(Builders<SupplierDocument>.Filter.Empty)
            .SortByDescending(supplier => supplier.Id)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastSupplier == null)
        {
            return 1;
        }

        return lastSupplier.Id + 1;
    }

    private async Task<long> GetMaxSupplierCodeNumberAsync(CancellationToken cancellationToken)
    {
        var projection = Builders<SupplierDocument>.Projection.Expression(supplier => supplier.Codigo);
        var codes = await _suppliers
            .Find(Builders<SupplierDocument>.Filter.Empty)
            .Project(projection)
            .ToListAsync(cancellationToken);

        var max = 0;
        foreach (var code in codes)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length < 4)
            {
                continue;
            }

            var numberPart = code.Substring(code.Length - 3);
            if (!int.TryParse(numberPart, out var parsed))
            {
                continue;
            }

            if (parsed > max)
            {
                max = parsed;
            }
        }

        return max + 1;
    }

    private static Supplier MapToDomain(SupplierDocument document)
    {
        return new Supplier
        {
            Id = document.Id,
            Codigo = document.Codigo,
            RazonSocial = document.RazonSocial,
            Direccion = document.Direccion,
            Contacto = document.Contacto,
            Rubro = document.Rubro,
            Telefono = document.Telefono
        };
    }

    private sealed class SupplierDocument
    {
        [BsonId]
        public long Id { get; set; }

        [BsonElement("code")]
        public string Codigo { get; set; } = string.Empty;

        [BsonElement("social_reason")]
        public string RazonSocial { get; set; } = string.Empty;

        [BsonElement("address")]
        public string Direccion { get; set; } = string.Empty;

        [BsonElement("contact")]
        public string Contacto { get; set; } = string.Empty;

        [BsonElement("area")]
        public string Rubro { get; set; } = string.Empty;

        [BsonElement("phone")]
        public string Telefono { get; set; } = string.Empty;

        [BsonElement("state")]
        public string State { get; set; } = ActiveState;

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class SequenceDocument
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        [BsonElement("nextValue")]
        public long NextValue { get; set; }
    }
}



