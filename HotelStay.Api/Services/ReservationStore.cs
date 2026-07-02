using System;
using System.Collections.Concurrent;
using HotelStay.Api.Models;

namespace HotelStay.Api.Services
{
    /// <summary>
    /// In-memory reservation store. Use ConcurrentDictionary for thread-safety in this demo.
    /// </summary>
    public sealed class ReservationStore
    {
        private readonly ConcurrentDictionary<string, ReservationConfirmation> _store = new();

        public bool TryAdd(string id, ReservationConfirmation confirmation) => _store.TryAdd(id, confirmation);

        public bool TryGet(string id, out ReservationConfirmation? confirmation) => _store.TryGetValue(id, out confirmation);
    }
}
