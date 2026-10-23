using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pharos.Server.Infrastructure;
using Pharos.Server.Instrument.Application.Services.Pairing;
using Pharos.Server.Instrument.Infrastructure.Grpc;

namespace Pharos.Server.Instrument.Infrastructure.Kestrel;

internal class ConfigureKestrel(
    IHostEnvironment environment,
    IConfiguration configuration,
    ICertificateStore certificateStore,
    ClientCertificateCache certificateCache,
    IOptions<GrpcSettings> grpcSettings) : IConfigureOptions<KestrelServerOptions>
{
    public void Configure(KestrelServerOptions options)
    {
        ValidateEndpointsConfig();

        ValidateCertificatesConfig();

        certificateStore.Initialize();

        options.ConfigureHttpsDefaults(https =>
        {
            https.ServerCertificateSelector = (_, serverName) => certificateStore.GetTlsCertificate(serverName);
        });

        var main = grpcSettings.Value.MainService;

        options.ListenAnyIP(main.Port, listen =>
        {
            listen.Protocols = HttpProtocols.Http2;
            if (main.UseHttps)
            {
                listen.UseHttps(https =>
                {
                    if (main.RequireClientCertificate)
                    {
                        https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                        https.ClientCertificateValidation = (clientCertificate, chain, policyErrors) =>
                        {
                            if (chain is null || policyErrors != SslPolicyErrors.None || chain.ChainElements.Count == 0)
                            {
                                return false;
                            }

                            var chainRoot = chain.ChainElements[^1].Certificate;
                            using var trustedRoot = certificateStore.GetCertificate(certificateStore.StoreLocation, StoreName.Root, chainRoot.Thumbprint);

                            var instrumentNumber = trustedRoot?.GetInstrumentNumber();
                            if (string.IsNullOrWhiteSpace(instrumentNumber))
                            {
                                return false;
                            }

                            certificateCache.Add(clientCertificate.Thumbprint, instrumentNumber);
                            return true;
                        };
                    }
                });
            }
        });

        options.ListenAnyIP(grpcSettings.Value.PairingService.Port, listen =>
        {
            listen.Protocols = HttpProtocols.Http2;
        });
    }

    private void ValidateCertificatesConfig()
    {
        var certificates = configuration.GetSection("Kestrel:Certificates").GetChildren().ToList();
        if (certificates.Count > 0)
        {
            throw new InvalidOperationException("Kestrel configuration must not define any certificates. Certificates are managed inside the code.");
        }
    }

    private void ValidateEndpointsConfig()
    {
        var endpoints = configuration.GetSection("Kestrel:Endpoints").GetChildren().ToList();

        var endpointUrls = endpoints.Select(endpoint => endpoint["Url"])
            .Select(url => Uri.TryCreate(url, UriKind.Absolute, out var endpointUrl)
                ? endpointUrl
                : throw new InvalidOperationException("Each Kestrel endpoint must have a valid absolute URL."))
            .ToList();

        var o = grpcSettings.Value;
        if (endpointUrls.Any(x => x.Port == o.MainService.Port || x.Port == o.PairingService.Port))
        {
            throw new InvalidOperationException("Kestrel configuration must not use the same ports as the gRPC services. gRPC endpoints are configured in the code.");
        }

        if (environment.IsProduction())
        {
            if (endpointUrls.Count == 0 || endpointUrls.All(endpointUrl => endpointUrl.IsLoopback))
            {
                throw new InvalidOperationException("Kestrel configuration must define at least one non-loopback endpoint.");
            }

            if (endpointUrls.Any(endpointUrl => !endpointUrl.IsLoopback && !string.Equals(endpointUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Every non-loopback Kestrel endpoint must use HTTPS.");
            }
        }
    }

}
