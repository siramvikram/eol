using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Octokit;

class Program
{
    static async Task Main(string[] args)
    {
        var osNames = GetOperatingSystemNames();
        var eolMessages = new List<string>();

        foreach (var os in osNames)
        {
            var apiResponse = await GetOperatingSystemInfo(os);
            eolMessages.AddRange(ValidateEol(os, apiResponse));
        }

        string issueTitle = $"EOL List - {DateTime.Today:yyyy-MM-dd} {DateTime.Now:HH:mm}";
        string issueBody = string.Join(Environment.NewLine, eolMessages);

        await CreateGitHubIssue(issueTitle, issueBody);
    }

    static HashSet<string> GetOperatingSystemNames() => new HashSet<string> { "alpine",
        "amazon-linux",
        "android"
       /* "bootstrap",
        "centos",
        "debian",
        "django",
        "dotnet",
        "dotnetcore",
        "dotnetfx",
        "drupal",
        "elasticsearch",
        "elixir",
        "fedora" */
    };

    static async Task<string> GetOperatingSystemInfo(string osName)
    {
        using (HttpClient client = new HttpClient())
        {
            string url = $"https://endoflife.date/api/{osName}.json";
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    static List<string> ValidateEol(string osName, string apiResponse)
    {
        var messages = new List<string>();
        JArray jsonArray = JArray.Parse(apiResponse);
        foreach (var item in jsonArray)
        {
            string eol = item["eol"]?.ToString();
            if (eol != null && DateTime.TryParse(eol, out DateTime eolDate))
            {
                DateTime today = DateTime.Today;
                if (eolDate <= today.AddMonths(6))
                {
                    int dayDiff = (eolDate - today).Days;
                    messages.Add($"OSName: {osName}, EOL: {eol}, Days until EOL: {dayDiff}");
                }
            }
        }
        return messages;
    }

    static async Task CreateGitHubIssue(string title, string body)
    {
        var client = new GitHubClient(new ProductHeaderValue("EOL-Issue-Creator"));
        var tokenAuth = new Credentials("xxxxx"); // Updated GitHub token
        client.Credentials = tokenAuth;

        var createIssue = new NewIssue(title)
        {
            Body = body
        };

        createIssue.Labels.Add("os-support");

        await client.Issue.Create("siramvikram", "Code", createIssue); // Updated repository
    }
}
