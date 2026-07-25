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
            using var login = new LoginForm(apiClient);
            if (login.ShowDialog() == DialogResult.OK && login.Autenticado)
                Application.Run(new MainForm(apiClient));
        }
    }
}
