using Tools.Api.Modules.Core.Realtime.Application.Services;

namespace Tools.Api.Modules.Core.Realtime.Application.Usecases;

// Action déclenchée par un appel de service à service, sans utilisateur à autoriser. La
// résolution des destinataires et le push vivent dans RealtimeEventService — un use case ne
// doit rester qu'un point d'entrée, jamais reporter cette logique dans le contrôleur qui
// l'appelle ni la dupliquer ici.
public sealed class PublishRealtimeEventUseCase(RealtimeEventService realtimeEventService)
{
    public Task Execute(PublishRealtimeEventCommand command) =>
        realtimeEventService.PublishAsync(command);
}
