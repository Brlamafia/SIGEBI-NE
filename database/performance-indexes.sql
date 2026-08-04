-- SIGEBI / Supabase PostgreSQL
-- Script idempotente de rendimiento. No es una migración de Entity Framework.
-- Ejecútelo una sola vez desde Supabase > SQL Editor.

CREATE INDEX IF NOT EXISTS "IX_SolicitudPrestamo_estado_fecha"
    ON "SolicitudPrestamo" (estado, fecha_solicitud DESC);
CREATE INDEX IF NOT EXISTS "IX_SolicitudPrestamo_usuario_fecha"
    ON "SolicitudPrestamo" (id_usuario, fecha_solicitud DESC);
CREATE INDEX IF NOT EXISTS "IX_SolicitudPrestamo_libro_estado"
    ON "SolicitudPrestamo" (id_libro, estado);

CREATE INDEX IF NOT EXISTS "IX_Notificaciones_usuario_fecha"
    ON "Notificaciones" (id_usuario, fecha_envio DESC);
CREATE INDEX IF NOT EXISTS "IX_Notificaciones_usuario_leida"
    ON "Notificaciones" (id_usuario, leida);

CREATE INDEX IF NOT EXISTS "IX_Prestamos_usuario_estado"
    ON "Prestamos" (id_usuario, estado);
CREATE INDEX IF NOT EXISTS "IX_Prestamos_libro_estado"
    ON "Prestamos" (id_libro, estado);
CREATE INDEX IF NOT EXISTS "IX_Prestamos_estado_vencimiento"
    ON "Prestamos" (estado, fecha_devolucion_esperada);

CREATE INDEX IF NOT EXISTS "IX_Multas_usuario_estado"
    ON "Multas" (id_usuario, estado);
CREATE INDEX IF NOT EXISTS "IX_Multas_estado_fecha"
    ON "Multas" (estado, fecha_generacion DESC);

CREATE INDEX IF NOT EXISTS "IX_Ejemplares_libro_estado"
    ON "Ejemplares" (id_libro, estado);

CREATE INDEX IF NOT EXISTS "IX_Auditoria_fecha"
    ON "Auditoria" (fecha DESC);
CREATE INDEX IF NOT EXISTS "IX_Auditoria_usuario_fecha"
    ON "Auditoria" (id_usuario, fecha DESC);
CREATE INDEX IF NOT EXISTS "IX_Auditoria_modulo_fecha"
    ON "Auditoria" (modulo, fecha DESC);

ANALYZE "SolicitudPrestamo";
ANALYZE "Notificaciones";
ANALYZE "Prestamos";
ANALYZE "Multas";
ANALYZE "Ejemplares";
ANALYZE "Auditoria";

