# Base de datos Supabase

SIGEBI trabaja directamente con el esquema PostgreSQL existente y mantiene **cero migraciones de Entity Framework**.

Después de crear el esquema y cargar los datos iniciales, ejecuta `performance-indexes.sql` desde el SQL Editor de Supabase. El script solamente agrega índices idempotentes para las consultas frecuentes y actualiza las estadísticas del optimizador; no elimina ni transforma datos.

La cadena de conexión y las credenciales no deben guardarse en Git. Configúralas mediante User Secrets o variables de entorno, como indica el README principal.
