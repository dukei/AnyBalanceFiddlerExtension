using System;
using System.Text.RegularExpressions;
using Fiddler;

[assembly: Fiddler.RequiredVersion("2.3.5.0")]

public class AnyBalanceExtension : IAutoTamper2
{
    public void AutoTamperRequestBefore(Session oSession)
    {
        //AnyBalanceDebugger
        if (oSession.oRequest.headers.Exists("abd-replace-3xx"))
        {
            oSession["abd-replace-3xx"] = "1";
            oSession.oRequest.headers.Remove("abd-replace-3xx");
        }

    }

    public void OnPeekAtResponseHeaders(Session oSession)
    {
        //AnyBalanceDebugger
        if (oSession["abd-replace-3xx"] == "1")
        {
            int[] redirectCodes = { 301, 302, 303, 307 };

            if (Array.IndexOf(redirectCodes, oSession.responseCode) >= 0){
                String loc = oSession.oResponse.headers["Location"];
                if (Regex.IsMatch(loc, "^https?://", RegexOptions.IgnoreCase)){
                    Regex re = new Regex("^(https?://[^/]+)", RegexOptions.IgnoreCase);
                    Match matchesDomain = re.Match(loc);
                    Match matchesUrl = re.Match(oSession.fullUrl);
                    String locDomain = matchesDomain.Success ? matchesDomain.Groups[1].Value : null;
                    String urlDomain = matchesUrl.Success ? matchesUrl.Groups[1].Value : null;
                    if (locDomain != null && urlDomain != null && locDomain.ToLower() != urlDomain.ToLower())
                    {
                        oSession.responseCode += 400;
                        FiddlerApplication.Log.LogString("AnyBalance: Fiddling redirect from " + urlDomain + " to " + loc + " with code " + oSession.responseCode);
                    }
            }
        }
    }
}

public void AutoTamperRequestAfter(Session oSession) { }
    public void AutoTamperResponseAfter(Session oSession) { }
    public void AutoTamperResponseBefore(Session oSession) { }
    public void OnBeforeReturningError(Session oSession) { }
    public void OnBeforeUnload() { }
    public void OnLoad() { }

}
