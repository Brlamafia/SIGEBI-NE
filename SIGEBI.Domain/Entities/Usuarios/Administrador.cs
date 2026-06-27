using System;
using SIGEBI.Domain.Base;

namespace SIGEBI.Domain.Entities.Usuarios
{
    public class Administrador : EntidadAuditable
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Correo { get; set; }

        // Si el administrador tiene una relación con la entidad Cargo:
        public int CargoId { get; set; }
        public Cargo Cargo { get; set; }

        // Puedes agregar más propiedades específicas del Administrador aquí
    }
}