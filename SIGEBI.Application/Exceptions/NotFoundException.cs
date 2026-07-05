using System;

namespace SIGEBI.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string name, object key)
            : base($"La entidad \"{name}\" con la clave ({key}) no fue encontrada.")
        {
        }
    }
}