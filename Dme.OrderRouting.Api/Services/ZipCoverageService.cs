using Dme.OrderRouting.Api.Services.Interfaces;

namespace Dme.OrderRouting.Api.Services;

public class ZipCoverageService : IZipCoverageService
{
    private const int BroadZipRangeThreshold = 10000;

    public bool ServesZip(string serviceZips, string customerZip)
    {
        if (string.IsNullOrWhiteSpace(serviceZips) || string.IsNullOrWhiteSpace(customerZip))
        {
            return false;
        }

        var normalizedCustomerZip = customerZip.Trim();

        foreach (var token in serviceZips.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (IsZipRange(token))
            {
                if (IsZipInRange(token, normalizedCustomerZip))
                {
                    return true;
                }

                continue;
            }

            if (string.Equals(token.Trim(), normalizedCustomerZip, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsZipRange(string value)
    {
        return value.Contains('-');
    }

    private static bool IsZipInRange(string range, string customerZip)
    {
        var parts = range.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var startZip) || !int.TryParse(parts[1], out var endZip) || !int.TryParse(customerZip, out var targetZip))
        {
            return false;
        }

        return targetZip >= startZip && targetZip <= endZip;
    }

    public bool IsBroadCoverageRange(string serviceZips)
    {
        foreach (var token in serviceZips.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!IsZipRange(token))
            {
                continue;
            }

            var parts = token.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
            {
                continue;
            }

            if (!int.TryParse(parts[0], out var startZip) || !int.TryParse(parts[1], out var endZip))
            {
                continue;
            }

            if (endZip - startZip >= BroadZipRangeThreshold)
            {
                return true;
            }
        }

        return false;
    }
}