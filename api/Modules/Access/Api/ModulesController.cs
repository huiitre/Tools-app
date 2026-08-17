using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Tools.Api.Modules.Access.Application;
using Tools.Api.Modules.Access.Application.Dto;
using Tools.Api.Modules.Access.Application.Usecases;

namespace Tools.Api.Modules.Access.Api;

// Modules fonctionnels de l'application et accès des utilisateurs à ces modules.
//
// Les routes imbriquées sous `/modules/{id}/users` ne manipulent pas une ressource mais la
// relation entre deux : quel utilisateur appartient à quel module, et avec quel rôle.
//
// Les use cases sont résolus par action ([FromServices]) : un use case sécurisé applique son
// contrôle dès sa construction, une action ne doit donc construire que celui dont elle se sert.
// Ici toutes les routes exigent ADMIN, mais la règle vaut pour tous les contrôleurs.
[ApiController]
[Route("modules")]
public class ModulesController : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ModuleDto>> List([FromServices] ListModulesUseCase listModulesUseCase)
    {
        return listModulesUseCase.Execute();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateModuleRequest request,
        [FromServices] CreateModuleUseCase createModuleUseCase)
    {
        var moduleId = await createModuleUseCase.Execute(
            new CreateModuleCommand(request.Code, request.Name, request.Description));

        return StatusCode(StatusCodes.Status201Created, new { id = moduleId });
    }

    [HttpPut("{moduleId:long}")]
    public async Task<IActionResult> Update(
        long moduleId,
        UpdateModuleRequest request,
        [FromServices] UpdateModuleUseCase updateModuleUseCase)
    {
        await updateModuleUseCase.Execute(new UpdateModuleCommand(
            moduleId,
            request.Code,
            request.Name,
            request.Description,
            request.Active));

        return NoContent();
    }

    [HttpGet("{moduleId:long}/users")]
    public Task<IReadOnlyList<ModuleMemberDto>> Members(
        long moduleId,
        [FromServices] ListModuleMembersUseCase listModuleMembersUseCase)
    {
        return listModuleMembersUseCase.Execute(moduleId);
    }

    [HttpPost("{moduleId:long}/users/{userId:long}")]
    public async Task<IActionResult> GrantAccess(
        long moduleId,
        long userId,
        [FromServices] GrantModuleAccessUseCase grantModuleAccessUseCase)
    {
        await grantModuleAccessUseCase.Execute(new GrantModuleAccessCommand(moduleId, userId));
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{moduleId:long}/users/{userId:long}/role")]
    public async Task<IActionResult> ChangeRole(
        long moduleId,
        long userId,
        ChangeModuleRoleRequest request,
        [FromServices] ChangeModuleRoleUseCase changeModuleRoleUseCase)
    {
        await changeModuleRoleUseCase.Execute(
            new ChangeModuleRoleCommand(moduleId, userId, request.RoleId));

        return NoContent();
    }

    [HttpDelete("{moduleId:long}/users/{userId:long}")]
    public async Task<IActionResult> RevokeAccess(
        long moduleId,
        long userId,
        [FromServices] RevokeModuleAccessUseCase revokeModuleAccessUseCase)
    {
        await revokeModuleAccessUseCase.Execute(new RevokeModuleAccessCommand(moduleId, userId));
        return NoContent();
    }
}

// DTO entrants : ASP.NET applique ces règles avant d'appeler l'action.
public sealed record CreateModuleRequest(
    [Required] string Code,
    [Required] string Name,
    string? Description);

public sealed record UpdateModuleRequest(
    [Required] string Code,
    [Required] string Name,
    string? Description,
    bool Active);

public sealed record ChangeModuleRoleRequest([Required] long RoleId);
