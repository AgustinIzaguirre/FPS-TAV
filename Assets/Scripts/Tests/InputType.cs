public enum InputType
{
    JUMP            = 1,
    LEFT            = (1 << 1),
    RIGHT           = (1 << 2),
    FORWARD         = (1 << 3),
    BACKWARD        = (1 << 4),
    SHOOT           = (1 << 5),
    ROTATE_LEFT     = (1 << 6),
    ROTATE_RIGHT    = (1 << 7),
    ROTATE_DOWN     = (1 << 8),
    ROTATE_UP       = (1 << 9)
}