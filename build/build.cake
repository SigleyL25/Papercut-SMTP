#tool "nuget:?package=MarkdownSharp&version=1.13.0"
#tool "nuget:?package=MimekitLite&version=2.0.6"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.9.0"
#tool "nuget:?package=OpenCover&version=4.6.519"

#reference "tools/MarkdownSharp.1.13.0.0/lib/35/MarkdownSharp.dll"
#reference "tools/MimeKitLite.2.0.6/lib/net45/MimeKitLite.dll"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "All");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx => Information("Running tasks..."));

Teardown(ctx => Information("Finished running tasks."));

///////////////////////////////////////////////////////////////////////////////
// Configuration
///////////////////////////////////////////////////////////////////////////////
const string MajorVersion = "5";
const string MinorVersion = "1";

var appBuildDir = Directory("../src/Papercut.UI/bin") + Directory(configuration);
var svcBuildDir = Directory("../src/Papercut.Service/bin") + Directory(configuration);
var testBuildDir = Directory("../test/Papercut.Module.WebUI.Tests/bin") + Directory(configuration);

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() =>
{
    CleanDirectory(appBuildDir);
    CleanDirectory(svcBuildDir);
    CleanDirectory(testBuildDir);
});

///////////////////////////////////////////////////////////////////////////////
Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("../Papercut.sln");
});

///////////////////////////////////////////////////////////////////////////////
Task("PatchAssemblyInfo")
    .Does(() =>
{
    var buildNo = AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Build.Number : 0;
    var commitId = AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Repository.Commit.Id : "LocalBuild";
    var version = string.Format("{0}.{1}.{2}.{3}", MajorVersion, MinorVersion, buildNo, 0);
    var semVersion = string.Format("{0} (Commit: {1}", version, commitId);

    CreateAssemblyInfo("../src/GlobalAssemblyInfo.cs", new AssemblyInfoSettings {
        Version = version,
        FileVersion = version,
        InformationalVersion = semVersion
    });   
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("CreateReleaseNotes")
    .Does(() => {

    Information("Transforming release notes");
    var notes = System.IO.File.ReadAllText(System.IO.Path.GetFullPath("..\\ReleaseNotes.md"));
    var markdown = new MarkdownSharp.Markdown();
    var html = markdown.Transform(notes);
    var outPut = @"<!DOCTYPE HTML>
<html><head><meta http-equiv=""content-Type"" content=""text/html; charset=UTF-8"">
<style>.subHeader { color: #A6A6A6; font-style: italic; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; font-size: 1em; }
h1 { color: #0070C0; font-style: normal; font-weight: bold; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; font-size: 2em; }
body { font-style: normal; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; font-size: 1em; }
li { margin-bottom: 5px; } </style></head><body><div><p><img width=""232"" height=""50"" 
src=""data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAOgAAAAyCAMAAABh0mcDAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAyJpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuMy1jMDExIDY2LjE0NTY2MSwgMjAxMi8wMi8wNi0xNDo1NjoyNyAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RSZWY9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZVJlZiMiIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIENTNiAoV2luZG93cykiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6RUU1MUVEQTk5MUQ2MTFFMjlGMDZGODZFMzUxN0Y1QzgiIHhtcE1NOkRvY3VtZW50SUQ9InhtcC5kaWQ6RUU1MUVEQUE5MUQ2MTFFMjlGMDZGODZFMzUxN0Y1QzgiPiA8eG1wTU06RGVyaXZlZEZyb20gc3RSZWY6aW5zdGFuY2VJRD0ieG1wLmlpZDpFRTUxRURBNzkxRDYxMUUyOUYwNkY4NkUzNTE3RjVDOCIgc3RSZWY6ZG9jdW1lbnRJRD0ieG1wLmRpZDpFRTUxRURBODkxRDYxMUUyOUYwNkY4NkUzNTE3RjVDOCIvPiA8L3JkZjpEZXNjcmlwdGlvbj4gPC9yZGY6UkRGPiA8L3g6eG1wbWV0YT4gPD94cGFja2V0IGVuZD0iciI/Ph8nKXQAAAMAUExURZOTk1JSU4WWoS93qff39zSCuTRXca2tre3t7fLy8vT09Pz8/ANWk42Njvj4+JGjsM3R1Glpa7G6wnR0dLGxsWxsbjB6ruTt9Nra2lNod93d3V1dXr6+vnmLl6WlpRV2u0qGsGVlZsfa6Lm5uvr6+uvy+NHR0S1zpLnAxTJ+tVqSutbW1uvr64WFhXajw9Pi7G2FlsXFxZiYmGpqbEOCrzo6O1CKtERERkhISoqKinl5e25ubzJ9s1h2jDs7PBBgmTxmhXx8fHl5fEl4ms7OzrPN3yxtnLfQ4efn5+Hh4d3o8MrKyikpKqChojw8Pae0vSxwn3BwcaioqTAwMT19q2BgYra2tujo6J6eni94q4+PkNnk7MHBwYuVnVhYWqbE2e/v8DmPzJu81GNjZCdhi6DA1rG4vYuyztTU1LG1uMfR2D09PvL3+jQ0NYGBgdbZ2yRtofb5+zJ/tixPaD8/QDZ6qkxMTnZ2eHmlxfn7/IatyC1/uzB0pEJCRHmUpzw8PgBGiWacw66vr3SFkEZGSOPj4zJ3qEpKSz4+P2GVuq/J3Imvy5O2z1paXF5eXzWJxTWGwChlkWWZvejr7UFBQkRtizZ/s21/jEJCQoqz0VZWV4Orx8/T1pWbn2B4iHyox2Z7iWZmZk9PUTmKxPv8/Shafd/f36WqrjF7sKrG2h5pnzc3OMLL0T91mz6CsoKCg2hoarKyszeGvipql4aGh256gjl3pBhlnW2Ajn5+f+Xn6GpqajlgfG2dvzB4rC10pipypGFzgDMzNC50pv///y50py51p+Tk5C5zpf7+/v39/bvQ37/T4YKv0OPg3idwo7u7vIuqwG+QqH+px/Pz83es0OXl5lyDnvP09GJ/lD+Arz95onGculpwgFN6lfn5+XNzcxpAXB9PcCNFXW6jyABBhqOkpfHx8cjMzzeMyOnp6enq6+rq6sPFx77HzKfC1OPl57/K0pubm8C/v/7+//7//9vn7zB2p0BXZkNfc9HPzdDU12SSszV8sGiSsP///yn8YwgAAAEAdFJOU////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////wBT9wclAAAMUElEQVR42uyaC1xb1R3HyZIIJIBGSKTQKhRKyqMPQmMCwdRaWkHgjld4tVJYqq3e0tJ4G+xs1NUU4gPEDq0ydSXais20oGitj6rkkmLmNpzMfdR1OKcwGc7Z2uEe7noeN8kluYHW+vm0Vf/9fNpzzz339Hzv/3/+53fOTQjzPbGQ7wRFspth3KIo6XccdHSUGc/LTB+NChNJZwGtz4upbe6O7+qKf2lzWfr5hekWMUzxyh3K59+dK5qsT54ZtLSoUa0nbRRF2W5zDNhCos4jdyYzKX9u6MjtMTV8dcf+8QmJdCbQaFJDkSRJEARJ2jotNiLvPMGUipioa7/6xDXkdDqVpoaVoeEZYaMzgTpseo7ZNI655wrK9thr57mD3QTp59OVDSalE9mQ8pMH9qSPVPE6lRdUr+8k484Fyk8fefSmD5+9PLg7JfPE9zqHnB5TpjY8NVafERY1GyhlJQkSk35wDnA+/GGJQiw2BQEFNEkrG3qUTo4NKe99Zm/KSNWIdEZQHSOVhFKIlCDOgeT7cEnHEZdz8PIg7sxY+sy9yiHndFP2NDz1yGRYRpQ7OCixApYL1YjUVnwOgN6XC4bOCwrWlBveb1AonQE2pOx47NE8SVW4dBZQppmCoGTz2Qf9LwJ1BoJKo5j8pc90KP3sVdapCh6n8oAOq9FFna/bkfBg67AonCcjukdGgjQPl0ybZOOi2Tya6nS+6krjkQhPvN/wVVPE8unWtK2paYp16hWZIxPj0hlBYweQS7VoPYpTxWvUjQNqTe9a7zP9htJSXftGhjlOlQ+oDSnTh1EmUA8MEPFJnuuNBl1paXs2KGUPFDkyPC/vhmMXXLBv38tzHmaYea/v2bNn796fw/rr57wALuZczzBbjt1x7NkeMGzh00ufe+H1eY9zZmfBu4917Dhw4O43r17Fsflph5YdONSCnFrT8Hzki1yn8oAeLe/0gFZaixy2TmSUo7WAfUZgAeKCGmYebOy0gD+OSA5mZvkAZekEtWpBPa5Z6+gE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"" alt=""""></p>
<p class=""subHeader"">The Simple Desktop Email Receiver.</p></div>" + html + "</body></html>";

    Information("Write readme mail");
    var body = new MimeKit.BodyBuilder { HtmlBody = outPut };
    var message = new MimeKit.MimeMessage
    {
        Subject = "Welcome to Papercut!",
        Body = body.ToMessageBody(),
    };
    message.From.Add(new MimeKit.MailboxAddress("Papercut", "Papercut@papercut.com"));
    message.To.Add(new MimeKit.MailboxAddress("", "Papercut@user.com"));
    message.WriteTo(System.IO.Path.GetFullPath("..\\src\\Papercut.UI\\Readme.eml"));    
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("Build")
    .Does(() =>
{
    MSBuild("../Papercut.sln", settings => settings
                            .SetConfiguration(configuration)
                            .SetVerbosity(Verbosity.Normal)
                            .SetPlatformTarget(PlatformTarget.MSIL)
                            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
                            .UseToolVersion(MSBuildToolVersion.Default)
                            .WithTarget("Build"));   
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("../test/**/bin/" + configuration + "/*.Test.dll", 
                    new NUnit3Settings { NoResults = true });
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("Package")
    .Does(() =>
{
    
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("All")
    .IsDependentOn("PatchAssemblyInfo")
    .IsDependentOn("Clean")
    .IsDependentOn("CreateReleaseNotes")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
    .OnError(exception => Error(exception));

RunTarget(target);