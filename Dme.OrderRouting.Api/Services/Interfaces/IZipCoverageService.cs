namespace Dme.OrderRouting.Api.Services.Interfaces;

public interface IZipCoverageService
{
    bool ServesZip(string serviceZips, string customerZip);
    bool IsBroadCoverageRange(string serviceZips);
}