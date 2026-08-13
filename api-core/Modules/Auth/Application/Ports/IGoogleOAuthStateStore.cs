// Stocke temporairement l'état OAuth afin de lier un callback à l'ouverture initiale du navigateur.
public interface IGoogleOAuthStateStore
{
    string Create(string source);
    string Consume(string state);
}
