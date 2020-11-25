using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Zone
{
    private Vector3 bottomLeft;
    private Vector3 topLeft;
    private Vector3 bottomRight;
    private Vector3 topRight;

    public Zone(Vector3 bottomLeft, Vector3 topLeft, Vector3 bottomRight, Vector3 topRight)
    {
        this.bottomLeft = bottomLeft;
        this.topLeft = topLeft;
        this.bottomRight = bottomRight;
        this.topRight = topRight;
    }

    public Vector3 GetBottomLeft()
    {
        return bottomLeft;
    }
    
    public Vector3 GetTopLeft()
    {
        return topLeft;
    }
    
    public Vector3 GetBottomRight()
    {
        return bottomRight;
    }
    public Vector3 GetTopRight()
    {
        return topRight;
    }

    public float GetMinX()
    {
        float minX = Math.Min(bottomLeft.x, bottomRight.x);
        minX = Math.Min(minX, Math.Min(topLeft.x, topRight.x));
        return minX;
    }
    public float GetMaxX()
    {
        float maxX = Math.Max(bottomLeft.x, bottomRight.x);
        maxX = Math.Max(maxX, Math.Max(topLeft.x, topRight.x));
        return maxX;
    }
    
    public float GetMinY()
    {
        float minY = Math.Min(bottomLeft.y, bottomRight.y);
        minY = Math.Min(minY, Math.Min(topLeft.y, topRight.y));
        return minY;
    }
    
    public float GetMaxY()
    {
        float maxY = Math.Max(bottomLeft.y, bottomRight.y);
        maxY = Math.Max(maxY, Math.Max(topLeft.y, topRight.y));
        return maxY;
    }
    
    public float GetMinZ()
    {
        float minZ = Math.Min(bottomLeft.z, bottomRight.z);
        minZ = Math.Min(minZ, Math.Min(topLeft.z, topRight.z));
        return minZ;
    }
    
    public float GetMaxZ()
    {
        float maxZ = Math.Max(bottomLeft.z, bottomRight.z);
        maxZ = Math.Max(maxZ, Math.Max(topLeft.z, topRight.z));
        return maxZ;
    }

    public Vector3 GetRandomPosition()
    {
        float xPosition = Random.Range(GetMinX(), GetMaxX());
        float yPosition = Random.Range(GetMinY(), GetMaxY());
        float zPosition = Random.Range(GetMinZ(), GetMaxZ());
        return new Vector3(xPosition, yPosition, zPosition);
    }
}
