using FluentAssertions;
using Dme.OrderRouting.Api.Services;

namespace Dme.OrderRouting.Tests.Services;

public class ZipCoverageServiceTests
{
    private readonly ZipCoverageService _service = new();

    [Fact]
    public void ServesZip_ShouldReturnTrue_ForExplicitZip()
    {
        var result = _service.ServesZip("10001,10002,10003", "10002");

        result.Should().BeTrue();
    }

    [Fact]
    public void ServesZip_ShouldReturnTrue_ForZipRange()
    {
        var result = _service.ServesZip("10001-10100", "10050");

        result.Should().BeTrue();
    }

    [Fact]
    public void ServesZip_ShouldReturnFalse_WhenZipNotCovered()
    {
        var result = _service.ServesZip("10001-10100", "20001");

        result.Should().BeFalse();
    }

    [Fact]
    public void ServesZip_ShouldReturnFalse_WhenServiceZipsIsEmpty()
    {
        var result = _service.ServesZip("", "10015");

        result.Should().BeFalse();
    }

    [Fact]
    public void ServesZip_ShouldReturnFalse_WhenCustomerZipIsEmpty()
    {
        var result = _service.ServesZip("10001-10100", "");

        result.Should().BeFalse();
    }

    [Fact]
    public void ServesZip_ShouldReturnFalse_WhenRangeIsMalformed()
    {
        var result = _service.ServesZip("10001-", "10015");

        result.Should().BeFalse();
    }

    [Fact]
    public void ServesZip_ShouldReturnFalse_WhenRangeContainsNonNumericZip()
    {
        var result = _service.ServesZip("10001-ABCDE", "10015");

        result.Should().BeFalse();
    }

    [Fact]
    public void ServesZip_ShouldReturnTrue_WhenExplicitZipHasWhitespace()
    {
        var result = _service.ServesZip("10001, 10015, 10030", "10015");

        result.Should().BeTrue();
    }
}