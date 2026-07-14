namespace SIGEBI.Application.Dtos.Catalogo
{
    public class LibroDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public string Editorial { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}
