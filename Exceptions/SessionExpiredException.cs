using System;

namespace Sistema_de_Gestion_de_Importaciones.Exceptions
{
    public class SessionExpiredException : Exception
    {
        public SessionExpiredException() : base("La sesión ha expirado") { }
        public SessionExpiredException(string message) : base(message) { }
        public SessionExpiredException(string message, Exception innerException) : base(message, innerException) { }
    }
}