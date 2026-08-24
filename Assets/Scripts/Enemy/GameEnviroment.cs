using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public sealed class GameEnviroment
{
    private static GameEnviroment instance;
    private List<GameObject> Checkpoints = new List<GameObject>();
    public List<GameObject> checkpoints { get { return Checkpoints; } }
    public static GameEnviroment Singleton
    {
        get
        {
            if (instance == null)
            {
                instance = new GameEnviroment();
                instance.Checkpoints.AddRange(GameObject.FindGameObjectsWithTag("Checkpoint"));
            }
            return instance;
        }
    }
}
