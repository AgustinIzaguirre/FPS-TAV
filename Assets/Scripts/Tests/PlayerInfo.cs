using System.Net;

public class PlayerInfo
{
    public static float MIN_DAMAGE = 0f;
    public static float MAX_DAMAGE = 100f;
    public static float MIN_LIFE = 0f;
    public static float MAX_LIFE = 100f;

    public int id;
    public PlayerEntity playerEntity;
    public IPEndPoint endPoint;
    public int lastInputApplied;
    public float life;
    public float damage;
    public bool isShooting;

    public PlayerInfo(int id, PlayerEntity player, int lastInputApplied, float life, float damage, bool isShooting,
        IPEndPoint endPoint)
    {
        this.id = id;
        this.playerEntity = player;
        this.endPoint = endPoint;
        this.lastInputApplied = lastInputApplied;
        this.life = life;
        this.damage = damage;
        this.isShooting = isShooting;
    }
}