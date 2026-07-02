using HotelStay.Api.Validation;
using HotelStay.Api.Models;
using Xunit;

namespace HotelStay.Tests
{
    public class DocumentValidatorTests
    {
        [Theory]
        [InlineData("NYC")]
        [InlineData("LAX")]
        public void Validate_Domestic_Allows_NationalId_And_Passport(string city)
        {
            var r1 = DocumentValidator.Validate(city, DocumentType.NationalId);
            Assert.True(r1.IsValid);

            var r2 = DocumentValidator.Validate(city, DocumentType.Passport);
            Assert.True(r2.IsValid);
        }

        [Theory]
        [InlineData("LON")]
        [InlineData("TYO")]
        [InlineData("PAR")]
        public void Validate_International_Requires_Passport(string city)
        {
            var r1 = DocumentValidator.Validate(city, DocumentType.NationalId);
            Assert.False(r1.IsValid);
            Assert.Equal("PassportRequired", r1.Error?.Code);

            var r2 = DocumentValidator.Validate(city, DocumentType.Passport);
            Assert.True(r2.IsValid);
        }

        [Fact]
        public void Validate_UnknownDestination_Requires_Passport()
        {
            var r = DocumentValidator.Validate("UNKNOWN_CITY", DocumentType.NationalId);
            Assert.False(r.IsValid);
            Assert.Equal("UnknownDestinationPassportRequired", r.Error?.Code);

            var r2 = DocumentValidator.Validate("UNKNOWN_CITY", DocumentType.Passport);
            Assert.True(r2.IsValid);
        }
    }
}
