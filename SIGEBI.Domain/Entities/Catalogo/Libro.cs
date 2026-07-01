using System;
using SIGEBI.Domain.Base;
// B.R
namespace SIGEBI.Domain.Entities.Catalogo
{
    // Representa la obra intelectual dentro del catalogo
    public class Libro : EntidadAuditable
    {
        public string Titulo { get; private set; } = string.Empty;
        public string Autor { get; private set; } = string.Empty;
        public string ISBN { get; private set; } = string.Empty;
        public string Genero { get; private set; } = string.Empty;
        public string Editorial { get; private set; } = string.Empty;
        public string Estado { get; private set; } = "Disponible";

        private Libro() { }

        public Libro(string titulo, string autor, string isbn, string genero, string editorial)
        {
            Titulo = ValidarTextoObligatorio(titulo, nameof(titulo));
            Autor = ValidarTextoObligatorio(autor, nameof(autor));
            ISBN = ValidarTextoObligatorio(isbn, nameof(isbn));
            Genero = genero?.Trim() ?? string.Empty;
            Editorial = editorial?.Trim() ?? string.Empty;
        }

        public void ActualizarDetalles(string titulo, string autor, string genero, string editorial)
        {
            Titulo = ValidarTextoObligatorio(titulo, nameof(titulo));
            Autor = ValidarTextoObligatorio(autor, nameof(autor));
            Genero = genero?.Trim() ?? string.Empty;
            Editorial = editorial?.Trim() ?? string.Empty;
            MarcarComoModificada();
        }

        public void Descatalogar()
        {
            Estado = "Descatalogado";
            MarcarComoModificada();
        }

        private static string ValidarTextoObligatorio(string valor, string nombreParametro)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("Este campo es obligatorio.", nombreParametro);
            return valor.Trim();
        }
    }
}