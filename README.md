# Mercadito

- `ServicioFrontend`
- `ServicioUsuarios` (PostgreSQL + `user-secrets`)
- `ServicioCatalogo` (MySQL + `user-secrets`)
- `ServicioEmpleados` (PostgreSQL + `user-secrets`)
- `ServicioProveedores` (MongoDB + `user-secrets`)

`legacy/ServicioVentas` esta desactivado por ahora.

## 1) Pre-chequeo de estados

- MySQL: `127.0.0.1:3306`
- PostgreSQL Users: `127.0.0.1:5432`
- PostgreSQL Empleados: `127.0.0.1:5412`
- MongoDB: `127.0.0.1:27017`

Revisar con PowerShell:
```powershell
Test-NetConnection 127.0.0.1 -Port 3306
Test-NetConnection 127.0.0.1 -Port 5432
Test-NetConnection 127.0.0.1 -Port 5412
Test-NetConnection 127.0.0.1 -Port 27017
```

## 2) Creación e inicializacion de las bases de datos

### MySQL (Catalogo, puerto 3306)
```powershell
mysqld --initialize --basedir="$env:MYSQL_BASEDIR" --datadir="$env:MYSQL_DATADIR" --console
# mysqld --basedir="$env:MYSQL_BASEDIR" --datadir="$env:MYSQL_DATADIR" --console
mysql -u root -p
ALTER USER 'root'@'localhost' IDENTIFIED BY 'mercadito';
CREATE DATABASE IF NOT EXISTS mercadito_db_catalog CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### PostgreSQL (Empleados y Usuarios; puerto 5412 para Empleados y puerto 5432 para Usuarios)
```powershell
initdb -D "$env:PGDATA_EMPLOYEES" -U postgres -A scram-sha-256 -W
# pg_ctl -D "$env:PGDATA_EMPLOYEES" -l "$env:PGLOGS_EMPLOYEES" -o "-p 5412" start
psql -U postgres -p 5412 -d postgres
CREATE DATABASE mercadito_db_employees;
pg_ctl -D "$env:PGDATA_EMPLOYEES" stop -m fast

initdb -D "$env:PGDATA_USERS" -U postgres -A scram-sha-256 -W
# pg_ctl -D "$env:PGDATA_USERS" -l "$env:PGLOGS_USERS" -o "-p 5432" start
psql -U postgres -p 5432 -d postgres
CREATE DATABASE mercadito_db_users;
pg_ctl -D "$env:PGDATA_USERS" stop -m fast
```

### MongoDB (Proveedores, puerto 27017)
```powershell
# Crear el archivo de configuracion:
mongod.conf
storage:
  dbPath: "$env:MONGO_DATA_DIR"

systemLog:
  destination: file
  path: "$env:MONGO_LOG_DIR/mongod.log"
  logAppend: true

net:
  bindIp: 127.0.0.1
  port: 27017

# Inicializar/reiniciar:
mongod --config "$env:MONGO_CONFIG"

# Entrar a la consola
mongosh

# Dentro de mongosh:
use admin

db.createUser({
  user: "admin",
  pwd: "mercadito",
  roles: [
    { role: "root", db: "admin" }
  ]
})

use mercadito_db_suppliers

db.createUser({
  user: "suppliers",
  pwd: "mercadito",
  roles: [
    { role: "readWrite", db: "mercadito_db_suppliers" }
  ]
})

# Luego activar en mongod.conf:
 security:
   authorization: enabled

# Entrar con la cuenta:
mongosh -u admin -p mercadito --authenticationDatabase admin
```

## 3) Cargar esquemas y datos de prueba

### Catalogo (MySQL)
```powershell
mysql -h 127.0.0.1 -P 3306 -u root -p mercadito_db_catalog
source schema/mercadito_mysql_catalogo_schema.sql;
source schema/mercadito_mysql_catalogo_sample.sql;
```

### Empleados (PostgreSQL)
```powershell
psql -h 127.0.0.1 -p 5412 -U postgres -d mercadito_db_employees -a -f schema/mercadito_postgres_empleados_schema.sql
psql -h 127.0.0.1 -p 5412 -U postgres -d mercadito_db_employees -a -f schema/mercadito_postgres_empleados_sample.sql
```

### Usuarios (PostgreSQL)
```powershell
psql -h 127.0.0.1 -p 5432 -U postgres -d mercadito_db_users -f schema/mercadito.user-service.sql
```

### Proveedores (MongoDB)
```powershell
(no auth) mongosh "mongodb://127.0.0.1:27017/mercadito_db_suppliers" --file schema/mercadito_mongo_proveedores_sample.js
mongosh "mongodb://admin:mercadito@127.0.0.1:27017/mercadito_db_suppliers?authSource=admin" --file schema/mercadito_mongo_proveedores_sample.js
```

## 4) Configurar `user-secrets` de los servicios

### ServicioUsuarios
```powershell
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "ConnectionStrings:PostgresConnection" "Host=127.0.0.1;Port=5432;Database=mercadito_db_users;Username=postgres;Password=mercadito"
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Jwt:Issuer" "ServicioUsuarios"
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Jwt:Audience" "ServicioFrontend"
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Jwt:Key" "zG8vKq9L2mP7xR4tY6nB3cD5fH1jS0aW9eU2iO7pQ4rT8mN6"
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Jwt:ExpiresMinutes" "60"
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Frontend:BaseUrl" "http://localhost:5038"

dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Smtp:Host" "localhost"
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Smtp:Port" "1025"
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Smtp:Username" " "
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Smtp:Password" " "
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Smtp:FromAddress" "no-reply@mercadito.local"
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Smtp:FromName" "Mercadito"
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj set "Smtp:UseSsl" "false"
```

### ServicioCatalogo
```powershell
dotnet user-secrets --project ServicioCatalogo/ServicioCatalogo.Api.csproj set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1;Port=3306;Database=mercadito_db_catalog;User ID=root;Password=mercadito;SslMode=None;AllowPublicKeyRetrieval=True"
```

### ServicioEmpleados
```powershell
dotnet user-secrets --project ServicioEmpleados/ServicioEmpleados.Api.csproj set "ConnectionStrings:DefaultConnection" "Host=127.0.0.1;Port=5412;Database=mercadito_db_employees;Username=postgres;Password=mercadito"
```

### ServicioProveedores
```powershell
(con auth) dotnet user-secrets --project ServicioProveedores/ServicioProveedores.Api.csproj set "ConnectionStrings:DefaultConnection" "mongodb://suppliers:mercadito@127.0.0.1:27017/mercadito_db_suppliers?authSource=mercadito_db_suppliers"
(sin auth) dotnet user-secrets --project ServicioProveedores/ServicioProveedores.Api.csproj set "ConnectionStrings:DefaultConnection" "mongodb://127.0.0.1:27017"
dotnet user-secrets --project ServicioProveedores/ServicioProveedores.Api.csproj set "Mongo:Database" "mercadito_db_suppliers"
dotnet user-secrets --project ServicioProveedores/ServicioProveedores.Api.csproj set "Mongo:SuppliersCollection" "suppliers"
dotnet user-secrets --project ServicioProveedores/ServicioProveedores.Api.csproj set "Mongo:SequencesCollection" "sequences"
```

### Validar secretos cargados
```powershell
dotnet user-secrets --project ServicioUsuarios/ServicioUsuarios.Api.csproj list
dotnet user-secrets --project ServicioCatalogo/ServicioCatalogo.Api.csproj list
dotnet user-secrets --project ServicioEmpleados/ServicioEmpleados.Api.csproj list
dotnet user-secrets --project ServicioProveedores/ServicioProveedores.Api.csproj list
```

## 5) Bootstrap admin de `ServicioUsuarios` (una sola vez)

Primero levantamos `ServicioUsuarios`:
```powershell
dotnet run --project ServicioUsuarios/ServicioUsuarios.Api.csproj --launch-profile http
```

Luego en otra terminal:
```powershell
$gen = Invoke-RestMethod http://localhost:5078/api/auth/generate-password; $gen
```

Se debe usar `$gen.hash` para insertar admin en PostgreSQL:

Importante:
- `mercadito_db_users` = **nombre de la base de datos**
- `users` = **nombre de la tabla** dentro de esa base de datos

```powershell
psql -U postgres -p 5432 -d mercadito_db_users
```

Nos conectamos a `mercadito_db_users` y ejecutamos:
```sql
INSERT INTO users
(username, email, password, role, creator_id, last_login, need_change_password, state, created_at, updated_at)
VALUES
('admin', 'admin@local', '<PEGA_AQUI_$gen.hash>', 'Admin', NULL, NULL, true, 'Active', NOW(), NOW());
```

La contraseña de login será `$gen.password`.

## 6) Levantar cada servicio

1. `ServicioUsuarios`
2. `ServicioCatalogo`
3. `ServicioEmpleados`
4. `ServicioProveedores`
5. `ServicioFrontend`

### Usuarios
```powershell
dotnet run --project ServicioUsuarios/ServicioUsuarios.Api.csproj --launch-profile http
```

### Catalogo
```powershell
dotnet run --project ServicioCatalogo/ServicioCatalogo.Api.csproj --launch-profile http
```

### Empleados
```powershell
dotnet run --project ServicioEmpleados/ServicioEmpleados.Api.csproj --launch-profile http
```

### Proveedores
```powershell
dotnet run --project ServicioProveedores/ServicioProveedores.Api.csproj --launch-profile http
```

### Frontend
```powershell
dotnet run --project ServicioFrontend/ServicioFrontend.Web.csproj --launch-profile ServicioFrontend
```

### Cliente SMTP
```powershell
mailpit --smtp 127.0.0.1:1025 --listen 127.0.0.1:8025
```

## 7) Smoke test

```powershell
Invoke-RestMethod http://localhost:5078/health
Invoke-RestMethod http://localhost:5150/health
Invoke-RestMethod http://localhost:5200/health
Invoke-RestMethod http://localhost:5250/health
```

## 8) URLs de referencia

- Frontend: `http://localhost:5038`
- Users API (Swagger): `http://localhost:5078/swagger`
- Catalog API (Swagger): `http://localhost:5150/swagger`
- Empleados API (Swagger): `http://localhost:5200/swagger`
- Proveedores API (Swagger): `http://localhost:5250/swagger`

## 9) SMTP (importante)

Si el `Smtp:*` apunta a datos falsos, el registro de usuarios desde API puede fallar y hacer rollback.

Para pruebas rápidas:
- un SMTP de testing real, o
- evita crear usuarios por endpoint hasta configurar SMTP.