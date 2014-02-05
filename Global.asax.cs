using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Funq;
using ServiceStack;
using ServiceStack.Mvc;

namespace MvcAsyncTests
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            //WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            new AppHost().Init();
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost() : base("MVC Async Tests", typeof(PdfService).Assembly) {}

        public override void Configure(Container container)
        {
            container.Register(c => new ImagingService("http://localhost:55472/api/"));

            ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(container));
        }
    }

    public class ImagingService : JsonServiceClient
    {
        public ImagingService(string url)
            : base(url)
        {
            //Headers.Add("X-ApiKey", "blah ");  //This does not work on Async methods, but is should.
            RequestFilter += (request) =>
            {
                request.Headers.Add("X-ApiKey", "BLAH");
            };
        }

        public byte[] GetBytes(string relativePath)
        {
            return Get<byte[]>(relativePath);
        }

        public async Task<byte[]> GetBytesAsync(string relativePath)
        {
            return await GetAsync<byte[]>(relativePath);
        }

        public byte[] GetDocumentAsPdf(Guid documentGuid)
        {
            var request = new RetrieveImageAsFormatRequest { DocumentNameGuid = documentGuid, Format = DocumentFormatType.Pdf };
            return Get<byte[]>(request);
        }

        public async Task<byte[]> GetDocumentAsPdfAsync(Guid documentGuid)
        {
            var request = new RetrieveImageAsFormatRequest { DocumentNameGuid = documentGuid, Format = DocumentFormatType.Pdf };
            return await GetAsync<byte[]>(request);
        }
    }

    [Route("/pdfservice")]
    public class RetrieveImageAsFormatRequest
    {
        public Guid DocumentNameGuid { get; set; }
        public DocumentFormatType Format { get; set; }
    }

    public enum DocumentFormatType
    {
        Pdf,
    }

    public class PdfService : Service
    {
        // GET: /api/pdfservice
        public object Any(RetrieveImageAsFormatRequest request)
        {
            var aspReq = (HttpRequestBase)Request.OriginalRequest;

            return new HttpResult(new FileInfo(aspReq.PhysicalApplicationPath.CombineWith("sample.pdf")));
        }
    }

    public class PdfController : Controller
    {
        public ImagingService ImagingService { get; set; }

        // GET: /Pdf/
        public async Task<ActionResult> Index()
        {
            return File(await ImagingService.GetBytesAsync("sample.pdf"), "application/pdf");
        }

        // GET: /Pdf/AsyncRequest
        public async Task<ActionResult> AsyncRequest()
        {
            return File(await ImagingService.GetDocumentAsPdfAsync(Guid.NewGuid()), "application/pdf");
        }

    }

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return base.Content(
                @"<h1>Links</h1>
                 <a href='/sample.pdf'>Static pdf served by MVC</a><br/>
                 <a href='/api/sample.pdf'>Static pdf served by ServiceStack</a><br/>
                 <a href='/api/pdfservice'>ServiceStack Service returning a static PDF</a><br/>
                 <a href='/Pdf/'>MVC Controller async calling ServiceStack returning a static pdf</a><br/>
                 <a href='/Pdf/AsyncRequest'>MVC Controller async calling a ServiceStack Service returning a static pdf</a><br/>");
        }
    }

}