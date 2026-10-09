using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Web.Pages;

namespace Nordly.Tests;

public class ErrorPageTests
{
    [Test]
    public void OnGet_Uses_Request_Trace_Id_As_Safe_Reference()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "request-reference-123";
        var model = new ErrorModel(NullLogger<ErrorModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = httpContext }
        };

        model.OnGet();

        Assert.That(model.ShowRequestId, Is.True);
        Assert.That(model.RequestId, Is.EqualTo("request-reference-123"));
    }

    [Test]
    public void ShowRequestId_Is_False_When_No_Reference_Is_Available()
    {
        var model = new ErrorModel(NullLogger<ErrorModel>.Instance)
        {
            RequestId = null
        };

        Assert.That(model.ShowRequestId, Is.False);
    }
}
