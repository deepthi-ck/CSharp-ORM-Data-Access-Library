using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;

namespace OrmDataAccess.Shared
{
    /// <summary>
    /// Server-rendered Data Access Dashboard HTML using only BCL (no JS SPA).
    /// Works for all FE/BE TFMs including net48.
    /// </summary>
    public static class DashboardPage
    {
        public static string Render(
            VersionInfo version,
            object? stats,
            string? flash,
            DataAccessResponse? last = null,
            string formAction = "/ui/action")
        {
            if (version is null) throw new ArgumentNullException(nameof(version));
            var sb = new StringBuilder(4096);
            sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"/>");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
            sb.Append("<title>").Append(H(version.Application)).Append("</title>");
            sb.Append("<style>");
            sb.Append("body{font-family:Segoe UI,Tahoma,sans-serif;margin:0;background:#f4f6f8;color:#1a1a1a}");
            sb.Append("header{background:#0f3d68;color:#fff;padding:1.25rem 1.5rem}");
            sb.Append("main{max-width:920px;margin:1.5rem auto;padding:0 1rem}");
            sb.Append("section{background:#fff;border:1px solid #d8dee6;border-radius:8px;padding:1rem 1.25rem;margin-bottom:1rem}");
            sb.Append("h1{margin:0 0 .35rem;font-size:1.35rem}h2{margin:0 0 .75rem;font-size:1.05rem}");
            sb.Append(".meta{opacity:.9;font-size:.92rem}label{display:block;margin:.4rem 0 .2rem;font-weight:600}");
            sb.Append("input,button{font:inherit;padding:.45rem .6rem}input{width:100%;max-width:420px;box-sizing:border-box}");
            sb.Append("button{background:#0f3d68;color:#fff;border:0;border-radius:4px;cursor:pointer;margin-right:.4rem;margin-top:.6rem}");
            sb.Append(".flash{background:#e8f5e9;border:1px solid #a5d6a7;padding:.75rem;border-radius:6px;margin-bottom:1rem}");
            sb.Append(".err{background:#ffebee;border-color:#ef9a9a}pre{white-space:pre-wrap;word-break:break-word;background:#f7f9fc;padding:.75rem;border-radius:6px}");
            sb.Append("a{color:#0f3d68}</style></head><body>");
            sb.Append("<header><h1>").Append(H(version.Application)).Append("</h1>");
            sb.Append("<div class=\"meta\">Branch: <strong>").Append(H(version.Branch)).Append("</strong>");
            sb.Append(" | Frontend .NET / Framework: <strong>").Append(H(version.FrontendDotnet)).Append("</strong>");
            sb.Append(" | Backend .NET / Framework: <strong>").Append(H(version.BackendDotnet)).Append("</strong>");
            sb.Append(" | ORM: <strong>").Append(H(version.Orm)).Append("</strong></div></header><main>");

            if (!string.IsNullOrWhiteSpace(flash))
            {
                var css = flash!.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0
                          || flash.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
                          || flash.IndexOf("NOT_FOUND", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "flash err" : "flash";
                sb.Append("<div class=\"").Append(css).Append("\">").Append(H(flash)).Append("</div>");
            }

            if (last != null)
            {
                sb.Append("<section><h2>Last result</h2><pre>");
                sb.Append(H(JsonSerializer.Serialize(last, new JsonSerializerOptions { WriteIndented = true })));
                sb.Append("</pre></section>");
            }

            sb.Append("<section><h2>Entity operations (C# server-side)</h2>");
            sb.Append("<form method=\"post\" action=\"").Append(H(formAction)).Append("\">");
            sb.Append("<label for=\"key\">Key</label><input id=\"key\" name=\"key\" value=\"user:1001\" required/>");
            sb.Append("<label for=\"value\">Value</label><input id=\"value\" name=\"value\" value=\"Visvantha\"/>");
            sb.Append("<label for=\"kind\">Kind</label><input id=\"kind\" name=\"kind\" value=\"user\"/>");
            sb.Append("<label><input type=\"checkbox\" name=\"preferReplica\" value=\"true\"/> Prefer replica on GET</label>");
            sb.Append("<div>");
            sb.Append("<button type=\"submit\" name=\"action\" value=\"put\">PUT</button>");
            sb.Append("<button type=\"submit\" name=\"action\" value=\"get\">GET</button>");
            sb.Append("<button type=\"submit\" name=\"action\" value=\"delete\">DELETE</button>");
            sb.Append("<button type=\"submit\" name=\"action\" value=\"stats\">Refresh stats</button>");
            sb.Append("</div></form></section>");

            sb.Append("<section><h2>Statistics</h2><pre>");
            sb.Append(H(stats == null ? "{}" : JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true })));
            sb.Append("</pre></section>");

            sb.Append("<section><h2>JSON API</h2><ul>");
            sb.Append("<li><a href=\"/health\">/health</a></li>");
            sb.Append("<li><a href=\"/version\">/version</a></li>");
            sb.Append("<li><a href=\"/orm/stats\">/orm/stats</a></li>");
            sb.Append("</ul><p>Dashboard HTML is generated with C# BCL only (StringBuilder, WebUtility, JsonSerializer).</p></section>");
            sb.Append("</main></body></html>");
            return sb.ToString();
        }

        public static Dictionary<string, string> ParseFormUrlEncoded(string body)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(body)) return map;
            var pairs = body.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var idx = pair.IndexOf('=');
                var rawKey = idx < 0 ? pair : pair.Substring(0, idx);
                var rawVal = idx < 0 ? string.Empty : pair.Substring(idx + 1);
                var key = Uri.UnescapeDataString(rawKey.Replace('+', ' '));
                var val = Uri.UnescapeDataString(rawVal.Replace('+', ' '));
                if (!string.IsNullOrEmpty(key)) map[key] = val;
            }
            return map;
        }

        private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
