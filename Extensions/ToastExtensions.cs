using Microsoft.AspNetCore.Mvc;

namespace Sistema_de_Gestion_de_Importaciones.Extensions
{
    public static class ToastExtensions
    {
        public static void Success(this Controller controller, string message)
        {
            controller.TempData["Success"] = message;
        }

        public static void Error(this Controller controller, string message)
        {
            controller.TempData["Error"] = message;
        }

        public static void Warning(this Controller controller, string message)
        {
            controller.TempData["Warning"] = message;
        }

        public static void Info(this Controller controller, string message)
        {
            controller.TempData["Info"] = message;
        }
    }
}
