// ISTA 425 / INFO 525 Algorithms for Games
//
// Sample code file

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxScroller : MonoBehaviour
{
    [Tooltip("Camera to which parallax is relative")]
    public GameObject parallaxCamera;

    [Tooltip("Level of parallax for this depth layer")]
    public float parallaxLevel;

    // the GC provides useful I/O and utility methods
    GameObject GC;
    GameController eventSystem;

    float startPos;

    // Start is called before the first frame update
    void Start()
    {
        GC = GameObject.FindGameObjectWithTag("GameController");
        eventSystem = GC.GetComponent<GameController>();

        startPos = transform.position.x;

        SoundManager.Instance.PlaySound(SoundManager.SoundType.Night, true);
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: Part of the parallax scrolling algorithm may go here.
    }
}
