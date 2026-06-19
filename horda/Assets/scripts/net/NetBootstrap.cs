using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;

// Inicializa os Unity Gaming Services e faz login anônimo, uma única vez.
// Idempotente: chamar várias vezes é seguro.
public static class NetBootstrap
{
    public static bool Ready { get; private set; }

    public static async Task InitAsync()
    {
        if (Ready) return;

        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Ready = true;
    }
}
