namespace SIGEBI.Application.Dtos.Multas
{
    // DTO de entrada: agrupa los datos necesarios para pagar una multa.
    public class PagarMultaDto
    {
        public int MultaId { get; set; }
        public int UsuarioResponsableId { get; set; }
    }
}
