using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnPositionGenerator
{
    private static List<Zone> zones;
    private static float yPosition = 1f;


    public static void AddZone(Zone zone)
    {
        zones.Add(zone);
    }

    public static List<Zone> getZones()
    {
        return zones;
    }

    public static void InitializeZones()
    {
        zones = new List<Zone>();
        AddZone(new Zone(new Vector3(-8.06f, 0.4f, -21.9f),
                        new Vector3(-8.06f, 0.4f, -16.49f),
                        new Vector3(-0.59f, 0.4f, -21.9f),
                        new Vector3(-0.59f, 0.4f, -16.49f)));
        AddZone(new Zone(new Vector3(9.57f, 0.4f, -20.47f),
                            new Vector3(9.57f, 0.4f, -15.46f),
                        new Vector3(18.2f, 0.4f, -20.47f),
                            new Vector3(18.2f, 0.4f, -15.46f)));
        AddZone(new Zone(new Vector3(15.78f, 0.4f, -13.32f),
                            new Vector3(15.78f, 0.4f, -9.47f),
                        new Vector3(6.48f, 0.4f, -13.32f),
                            new Vector3(6.48f, 0.4f, -9.47f)));
        AddZone(new Zone(new Vector3(-3.43f, 0.4f, -4.62f),
                            new Vector3(-3.43f, 0.4f, 1.53f),
                        new Vector3(0.84f, 0.4f, -4.62f),
                            new Vector3(0.84f, 0.4f, 1.53f)));
        AddZone(new Zone(new Vector3(0.98f, 0.4f, -4.32f),
                            new Vector3(0.98f, 0.4f, -0.81f),
                        new Vector3(6.01f, 0.4f, -4.32f),
                            new Vector3(6.01f, 0.4f, -0.81f)));
        AddZone(new Zone(new Vector3(8.676f, 0.4f, 0.1f),
                            new Vector3(8.676f, 0.4f, 1.37f),
                        new Vector3(16.74f, 0.4f, 0.1f),
                            new Vector3(16.74f, 0.4f, 1.37f)));
    }

    private static Zone GetRandomZone()
    {
        int index = (Random.Range(0, 2 * zones.Count)) % zones.Count;
        return zones[index];
    }
    
    public static Vector3 GetSpawningPosition()
    {
        Zone randomZone = GetRandomZone();
        Vector3 randomPosition = GetRandomZone().GetRandomPosition();
        return new Vector3(randomPosition.x, yPosition, randomPosition.z);
    }
}