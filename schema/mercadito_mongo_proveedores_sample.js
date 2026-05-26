// ServicioProveedores (MongoDB 8.x)
// Seed script for local/portable environments.
// Usage:
// mongosh "mongodb://127.0.0.1:27017/mercadito_suppliers" --file schema/mercadito_mongo_proveedores_sample.js

const databaseName = "mercadito_db_suppliers";
const suppliersCollectionName = "suppliers";
const sequencesCollectionName = "sequences";

const serviceDb = db.getSiblingDB(databaseName);
const suppliers = serviceDb.getCollection(suppliersCollectionName);
const sequences = serviceDb.getCollection(sequencesCollectionName);

// Clean only this service database state.
suppliers.drop();
sequences.drop();

serviceDb.createCollection(suppliersCollectionName);
serviceDb.createCollection(sequencesCollectionName);

// Indexes expected by ServicioProveedores repository.
suppliers.createIndex(
  { code: 1 },
  {
    name: "ux_supplier_code",
    unique: true,
  },
);

suppliers.createIndex(
  { state: 1, social_reason: 1, _id: 1 },
  { name: "ix_suppliers_state_social_reason_id" },
);
suppliers.createIndex(
  { state: 1, code: 1 },
  { name: "ix_suppliers_state_code" },
);

const now = new Date();

suppliers.insertMany([
  {
    _id: NumberLong("1"),
    code: "PRV001",
    social_reason: "Distribuidora Norte",
    address: "Av Principal 123, Zona Centro",
    contact: "Carlos Paredes",
    area: "Alimentos secos",
    phone: "78901234",
    state: "A",
    created_at: now,
    updated_at: now,
  },
  {
    _id: NumberLong("2"),
    code: "PRV002",
    social_reason: "Lacteos del Valle",
    address: "Zona Mercado 45",
    contact: "Mariela Quispe",
    area: "Lacteos frescos",
    phone: "71234567",
    state: "A",
    created_at: now,
    updated_at: now,
  },
  {
    _id: NumberLong("3"),
    code: "PRV003",
    social_reason: "Aseo Hogar SRL",
    address: "Av Industrial 78",
    contact: "Luis Romero",
    area: "Limpieza hogar",
    phone: "76543210",
    state: "A",
    created_at: now,
    updated_at: now,
  },
  {
    _id: NumberLong("4"),
    code: "PRV004",
    social_reason: "Panificadora Central",
    address: "Calle Pan 12",
    contact: "Diana Rios",
    area: "Panaderia artesanal",
    phone: "79887766",
    state: "A",
    created_at: now,
    updated_at: now,
  },
  {
    _id: NumberLong("5"),
    code: "PRV005",
    social_reason: "Bebidas Bolivia",
    address: "Av Libertador 567",
    contact: "Roberto Perez",
    area: "Bebidas frias",
    phone: "72345678",
    state: "A",
    created_at: now,
    updated_at: now,
  },
  {
    _id: NumberLong("6"),
    code: "PRV006",
    social_reason: "Carnes del Oriente",
    address: "Zona Ramada 23",
    contact: "Maria Lopez",
    area: "Carnes premium",
    phone: "73456789",
    state: "A",
    created_at: now,
    updated_at: now,
  },
  {
    _id: NumberLong("7"),
    code: "PRV007",
    social_reason: "Congelados Uyuni",
    address: "Av Fabrica 90",
    contact: "Pedro Gomez",
    area: "Congelados mixtos",
    phone: "74567890",
    state: "A",
    created_at: now,
    updated_at: now,
  },
  {
    _id: NumberLong("8"),
    code: "PRV008",
    social_reason: "Snacks Importados",
    address: "Zona Shopping 34",
    contact: "Ana Torres",
    area: "Snacks gourmet",
    phone: "75678901",
    state: "I",
    created_at: now,
    updated_at: now,
  },
]);

// Sequence documents consumed by ServicioProveedores.
// nextValue=9 means the next generated code/id starts at 9.
sequences.insertMany([
  { _id: "supplier_id", nextValue: 9 },
  { _id: "supplier_code", nextValue: 9 },
]);

print("Seed Mongo completado para ServicioProveedores.");
printjson({
  database: databaseName,
  suppliers: suppliers.countDocuments({}),
  activeSuppliers: suppliers.countDocuments({ state: "A" }),
  nextCodePreview: "PRV009",
});
