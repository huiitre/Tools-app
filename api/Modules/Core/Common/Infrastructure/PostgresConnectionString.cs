using Npgsql;

namespace Tools.Api.Modules.Core.Common.Infrastructure;

// Construction de la chaîne de connexion PostgreSQL à partir des variables d'environnement.
//
// Les conteneurs reçoivent DB_HOST, DB_PORT, DB_NAME, DB_USERNAME et DB_PASSWORD séparément :
// aucun environnement déployé ne fournit de chaîne complète. Le développement local, lui,
// peut renseigner ConnectionStrings:Postgres dans appsettings.Local.json.
public static class PostgresConnectionString
{
    // Retourne null lorsque aucune variable n'est fournie, pour laisser l'appelant se rabattre
    // sur ConnectionStrings:Postgres. Un jeu de variables incomplet, en revanche, est une
    // erreur de configuration : la signaler au démarrage vaut mieux qu'une connexion qui
    // échoue à la première requête, sur une machine où personne ne regarde les logs.
    public static string? FromEnvironmentVariables(IConfiguration configuration)
    {
        var host = configuration["DB_HOST"];
        var portValue = configuration["DB_PORT"];
        var database = configuration["DB_NAME"];
        var username = configuration["DB_USERNAME"];
        var password = configuration["DB_PASSWORD"];

        var databaseVariables = new[] { host, portValue, database, username, password };

        if (databaseVariables.All(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        if (databaseVariables.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                "Les variables DB_HOST, DB_PORT, DB_NAME, DB_USERNAME et DB_PASSWORD doivent toutes être renseignées.");
        }

        if (!int.TryParse(portValue, out var port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException("La variable DB_PORT doit contenir un port PostgreSQL valide.");
        }

        return new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password
        }.ConnectionString;
    }
}
