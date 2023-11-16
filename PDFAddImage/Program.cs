using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PDFAddImage.Service;
using PDFAddImage.Setting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {

        services.AddScoped<PdfImageSetting>(p =>
        {
            var pdfSetting = new PdfImageSetting()
            {
                //SourceUrl = "https://scconline-url2pdf-staging.azurewebsites.net/api/GeneratePdf?code=afV4ewwehBJjw8v9Y80fSJdYjXVDzrwQ8xZ3LBxQ206fAzFusDmSYQ==",
                SourceUrl = "https://scconline-url2pdf-staging.azurewebsites.net/api/GeneratePdf?code=afV4ewwehBJjw8v9Y80fSJdYjXVDzrwQ8xZ3LBxQ206fAzFusDmSYQ==",
                LeftMargin = 20,
                RightMargin = 0,
                TopMargin = 18,
                ImageWidth = 85,
                ImageHeight = 55
            };
            return pdfSetting;
        });
        services.AddScoped<PdfStorageSetting>(p =>
        {
            var storageSetting = new PdfStorageSetting()
            {
                //FolderPath = "C:\\Users\\shres\\Desktop\\PdfProcess\\PDFAddImage\\PDF\\",
                FolderPath = Path.Combine(Environment.CurrentDirectory, "PDF"),
                Container = "",
                FileName = ""
            };
            return storageSetting;
        });

        services.AddScoped<HeaderSetting>(p =>
        {
            HeaderSetting headerSetting = new HeaderSetting()
            {
                line1 = "SCC Online Web Edition, © {0} EBC Publishing Pvt. Ltd.",
                line2 = "Page {1}        {2}",
                line3 = "Printed For: {3}",
                line4 = "{4}",
                line5 = "© {0} {5}",
                line6 = "------------------------------------------------------------------------------------------------------------------------------------------------------ ",
            };
            return headerSetting;
        });

        services.AddScoped<HeaderIndexSetting>(p =>
        {
            return new HeaderIndexSetting()
            {
                rectangleBaseWidth = 20f,
                rectangleHeight = 38f,
                indexFontSize = 26,
            };
        });

        services.AddScoped<IPdfService, PdfFolderService>();
    })
    .Build();

host.Run();
