using System;
using HotelStay.Api.Services;
using HotelStay.Api.Models;
using Xunit;

namespace HotelStay.Tests
{
    public class ReservationStoreTests
    {
        [Fact]
        public void TryAddAndGet_WorksAsExpected()
        {
            var store = new ReservationStore();
            var conf = new ReservationConfirmation { ReservationId = "R1", Success = true, Status = "Confirmed", TotalPrice = new Money(100m, "USD") };
            var added = store.TryAdd("R1", conf);
            Assert.True(added);

            var got = store.TryGet("R1", out var fetched);
            Assert.True(got);
            Assert.Equal("R1", fetched.ReservationId);
        }
    }
}
