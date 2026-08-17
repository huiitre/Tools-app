namespace Tools.ApiCore.Modules.Users.Domain;

public class User
{
    public long? Id { get; }
    public string Name { get; }

    private User(long? id, string name)
    {

        if (string.IsNullOrWhiteSpace(name)) throw new Exception("Nom obligatoire");

        if (id is not null && id <= 0) throw new Exception("Id non valide");

        Id = id;
        Name = name;
    }

    public static User Create(string name)
    {
        return new User(null, name);
    }

    public static User Rehydrate(long id, string name)
    {
        return new User(id, name);
    }
}