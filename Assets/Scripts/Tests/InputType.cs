public enum InputType
{
    JUMP     = 1,
    LEFT     = (1 << 1),
    RIGHT    = (1 << 2),
    FORWARD  = (1 << 3),
    BACKWARD = (1 << 4),
    SHOOT    = (1 << 5)
}