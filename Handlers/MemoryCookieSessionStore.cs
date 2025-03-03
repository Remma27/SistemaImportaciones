using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Concurrent;

namespace Sistema_de_Gestion_de_Importaciones.Handlers
{
    public class MemoryCookieSessionStore : ITicketStore
    {
        private readonly ConcurrentDictionary<string, AuthenticationTicket> _store = new();

        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var id = Guid.NewGuid().ToString();
            _store[id] = ticket;
            return Task.FromResult(id);
        }

        public Task<AuthenticationTicket?> RetrieveAsync(string key)
        {
            _store.TryGetValue(key, out var ticket);
            return Task.FromResult(ticket);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            _store[key] = ticket;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _store.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }
}