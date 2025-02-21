using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();

// Create an in-memory store for todos
var todos = new List<Todo>
{
    new Todo { Id = 1, Title = "Buy groceries", IsCompleted = false },
    new Todo { Id = 2, Title = "Walk the dog", IsCompleted = true }
};

app.MapGet("/", (HttpContext context, IAntiforgery antiforgery) =>
{
  return Results.Extensions.RazorSlice<HtmxWithRazorSlices.Slices.Index, List<Todo>>(todos);
});

app.MapGet("/todos/create", (HttpContext context, IAntiforgery antiforgery) =>
{
  var vm = new CreateFormViewModel
  {
    AntiForgeryToken = antiforgery.GetAndStoreTokens(context)
  };
  return Results.Extensions.RazorSlice<HtmxWithRazorSlices.Slices.TodoCreateForm, CreateFormViewModel>(vm);
});

app.MapPost("/todos", ([FromForm] Todo todo, HttpContext context, IAntiforgery antiforgery) =>
{
  if (string.IsNullOrWhiteSpace(todo.Title))
  {
    var vm = new CreateFormViewModel
    {
      Todo = todo,
      AntiForgeryToken = antiforgery.GetAndStoreTokens(context)
    };
    vm.Errors.Add("Title", "The Title field is required.");

    context.Response.StatusCode = 422;
    context.Response.Headers.Add("HX-Retarget", "#todo-form");
    context.Response.Headers.Add("HX-Reswap", "innerHTML");

    return Results.Extensions.RazorSlice<HtmxWithRazorSlices.Slices.TodoCreateForm, CreateFormViewModel>(vm) as IResult;
  }

  todo.Id = todos.Max(t => t.Id) + 1;
  todos.Add(todo);
  return Results.Extensions.RazorSlice<HtmxWithRazorSlices.Slices.TodoItem, Todo>(todo);
});

app.MapDelete("/todos/{id}", (int id) =>
{
  var todo = todos.Find(t => t.Id == id);
  if (todo == null)
  {
    return Results.NotFound();
  }
  todos.Remove(todo);
  return Results.Ok();
});

app.MapPatch("/todos/{id}/toggle", (int id) =>
{
  var todo = todos.Find(t => t.Id == id);
  if (todo == null)
  {
    return Results.NotFound();
  }
  todo.IsCompleted = !todo.IsCompleted;
  return Results.Extensions.RazorSlice<HtmxWithRazorSlices.Slices.TodoItem, Todo>(todo);
});

app.Run();

public class Todo
{
  public int Id { get; set; }
  public string? Title { get; set; }
  public bool IsCompleted { get; set; }
}

public class CreateFormViewModel
{
  public Todo? Todo { get; set; }
  public Dictionary<string, string> Errors { get; } = new Dictionary<string, string>();
  public required AntiforgeryTokenSet AntiForgeryToken { get; set; }
}