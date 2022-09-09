using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Environment.WebRootPath = Environment.GetEnvironmentVariable("file_path") ?? Directory.GetCurrentDirectory();
builder.Environment.WebRootFileProvider = new PhysicalFileProvider(builder.Environment.WebRootPath);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.Use(async (context, next) =>
{
    if(context.Request.Method == "GET")
    {
        var  path = context.Request.Path.ToUriComponent();
        var directoryPath = path.Substring(0, path.LastIndexOf('/')+1);
        var fileName = path.Substring(path.LastIndexOf('/') + 1);
        var directory = app.Environment.WebRootFileProvider.GetDirectoryContents(directoryPath);
        var file = directory.Where(i => i.Name == fileName).SingleOrDefault();
        if (file == null) context.Response.StatusCode = 404;
        else await context.Response.SendFileAsync(file);
        return;
    }
    else if(context.Request.Method == "POST")
    {
        var response = "";
        var form = await context.Request.ReadFormAsync();
        var files = form.Files;
        foreach (var file in files)
        {
            var directory = app.Environment.WebRootPath + "/" + file.Name + "/";
            Directory.CreateDirectory(directory);
            var path = directory + file.FileName;
            using (var stream = new FileStream(path, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
                response += "/" + file.Name + "/" + file.FileName + "\n";
            }
        }

        await context.Response.WriteAsync(response);
        return;
    }

    await next();
});

app.Run();