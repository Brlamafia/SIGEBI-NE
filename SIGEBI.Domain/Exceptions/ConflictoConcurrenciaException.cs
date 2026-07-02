namespace SIGEBI.Domain.Exceptions
{
    public sealed class ConflictoConcurrenciaException : DomainException
    {
        public ConflictoConcurrenciaException()
            : base("La información fue modificada por otra operación. Actualice los datos e inténtelo nuevamente.")
        {
        }

        public ConflictoConcurrenciaException(Exception innerException)
            : base(
                "La información fue modificada por otra operación. Actualice los datos e inténtelo nuevamente.",
                innerException)
        {
        }
    }
}
