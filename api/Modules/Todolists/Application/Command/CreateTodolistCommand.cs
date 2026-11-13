public sealed record CreateTodolistCommand(
    string Name,
    bool Active,
    bool Favorite,
    string ColorHex,
    long DisplayOrder
);