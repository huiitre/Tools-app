using Microsoft.AspNetCore.Mvc;

namespace Tools.Api.Modules.Todolists.Api;

[ApiController]
[Route("todolists")]
public class TodolistsController : ControllerBase
{
    //* createTodolist POST
    [HttpPost]
    public async Task<IActionResult> Create(
        // [FromServices] CreateTodolistUseCase createTodolistUseCase
        CreateTodolistCommand command
    )
    {

        var todolist = Domain.Todolist.Create(
            name: command.Name,
            colorHex: command.ColorHex,
            active: command.Active,
            favorite: command.Favorite,
            displayOrder: command.DisplayOrder
        );

        // await createTodolistUseCase.Execute();

        return Created();
    }

    //* listTodolists GET 

    //* updateTodolist PATCH

    //* deleteTodolist DELETE
}