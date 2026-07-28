namespace SIGEBI.Desktop
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            using var apiClient = new ApiClient();
            while (true)
            {
                using var login = new LoginForm(apiClient);
                if (login.ShowDialog() != DialogResult.OK ||
                    !login.Autenticado ||
                    login.Session is null)
                    break;

                using var main = new MainForm(apiClient, login.Session);
                Application.Run(main);
                apiClient.CerrarSesion();
                if (!main.CerrarSesionSolicitado)
                    break;
            }
        }
    }
}
