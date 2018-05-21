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

            if (Array.IndexOf(redirectCodes, oSession.responseCode) >= 0)
            {
                String loc = oSession.oResponse.headers["Location"];
                Regex re = new Regex("^(https?:)?//([^/]+)(.*)", RegexOptions.IgnoreCase);
                Match urlMatch = re.Match(oSession.fullUrl);
                Match locMatch = re.Match(loc);
                if (urlMatch.Success && locMatch.Success)
                {
                    String urlSchema = urlMatch.Groups[1].Value;
                    String urlDomain = urlMatch.Groups[2].Value;
                    String locSchema = locMatch.Groups[1].Value;
                    String locDomain = locMatch.Groups[2].Value;
                    if (String.IsNullOrEmpty(locSchema))
                        locSchema = urlSchema;

                    if (locDomain.ToLower() != urlDomain.ToLower() || locSchema.ToLower() != urlSchema.ToLower())
                    {
                        oSession.responseCode += 400;
                        //Rewrite location to include schema explicitly
                        oSession.oResponse["Location"] = locSchema + "//" + locDomain + locMatch.Groups[3].Value;
                        FiddlerApplication.Log.LogString("AnyBalance: Fiddling redirect from " + urlDomain + " to " + loc + " with code " + oSession.responseCode);
                    }
                } else if(!urlMatch.Success)
                {
                    FiddlerApplication.Log.LogString("AnyBalance: Could not extract schema and domain from  " + oSession.fullUrl);
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
