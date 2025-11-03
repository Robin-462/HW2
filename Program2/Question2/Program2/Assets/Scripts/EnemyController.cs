// ISTA 425 / INFO 525 Algorithms for Games
//
// Sample code file

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Tooltip("Raven prefab type")]
    public GameObject RavenType;

    [Tooltip("Max number of ravens that may be in play at any time")]
    public int MaxRavens = 8;

    [Tooltip("Max number of ravens that may enter the scene per second")]
    public float RavenRate = 4.0f;

    [Tooltip("Fireball prefab type")]
    public GameObject FireballType;

    [Tooltip("Max number of fireballs that may be in play at any time")]
    public int   MaxFireballs = 8;

    [Tooltip("Max number of fireballs that may enter the scene per second")]
    public float FireballRate = 4.0f;

    // maximum numbero of ravens that may be in play at a time
    private GameObject[] RavenArray;
    int ravenIndex = 0;

    // maximum numbero of fireballs that may be in play at a time
    private GameObject[] FireArray;
    int fireIndex = 0;

    // rate limiting timers for spawn method
    float ravenTime = 0.0f;
    float fireTime = 0.0f;

    // the GC provides useful I/O and utility methods
    GameObject GC;
    GameController eventSystem;

    // sample code to show use of box indicators
    private bool showIndicators = false;

    private void SpawnEnemy(ref GameObject[] array, GameObject type, Vector4 position, Quaternion rotation, Vector3 direction, 
                            Vector2 xRandRange, Vector2 yRandRange, ref int index, int maxObjs, ref float timer, float rate)
    {
        Movement mover;

        // check if the instantaneous rate meets constraint
        if (timer > 1.0f / rate)
        {
            timer = 0.0f;

            if (array[index] != null)
                GameObject.Destroy(array[index]);

            // this conditional prevents enemies from spawning over
            // still living objects.
            if (array[index] == null)
            {
                Vector4 pos = transform.localToWorldMatrix * position;

                Vector3 randPos = position;
                randPos.x = position.x + Random.Range(xRandRange.x, xRandRange.y);
                randPos.y = position.y + Random.Range(yRandRange.x, yRandRange.y);

                GameObject enemy = Instantiate(type, randPos, rotation) as GameObject;
                // set the parent object to be the enemies object 
                enemy.transform.parent = this.transform;
                array[index] = enemy;

                // set the direction that the fireball is facing
                // note: direction is relative to world space
                mover = array[index].GetComponent<Movement>();
                mover.direction = direction;

                //Debug.Log("Enemy direction is " + mover.direction);

                // increment object index
                index++;
                if (index >= maxObjs)
                    index = 0;

                // sample code demonstarting how to place and show AABBs on objects
                if (showIndicators)
                {
                    AABB aabb = eventSystem.CreateAABB(enemy.GetComponent<BoxCollider2D>());
                    eventSystem.CreateIndicator(aabb);
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        RavenArray = new GameObject[MaxRavens];
        FireArray  = new GameObject[MaxFireballs];

        GC = GameObject.FindGameObjectWithTag("GameController");
        eventSystem = GC.GetComponent<GameController>();
    }

    // Update is called once per frame
    void Update()
    {
        ravenTime += Time.deltaTime;
        fireTime  += Time.deltaTime;

        // spawn enemies: Ravens
        SpawnEnemy (ref RavenArray, RavenType,
                   // set Tworld position, rotation, and direction (heading)
                   new Vector4(0.0f, 0.0f, 0.0f, 1.0f), Quaternion.identity, new Vector3(-1.0f, 0.0f, 0.0f),
                   // spawning position randomized offsets (x, y)
                   new Vector2(10.0f, 10.0f), new Vector2(-4.0f, 5.0f),
                   // indices and rate counters
                   ref ravenIndex, MaxRavens, ref ravenTime, RavenRate);

        // spawn enemies: Fireballs
        SpawnEnemy (ref FireArray, FireballType,
                   // set Tworld position, rotation, and direction (heading)
                   new Vector4(0.0f, 0.0f, 0.0f, 1.0f), Quaternion.identity, new Vector3(-1.0f,-1.0f, 0.0f),
                   // spawning position randomized offsets (x, y)
                   new Vector2(0.0f, 20.0f), new Vector2(5.0f, 5.0f),
                   // indices and rate counters
                   ref fireIndex, MaxFireballs, ref fireTime, FireballRate);

        // sample code to place AABBs and show Box Indicators on (new) moving objects
        if (eventSystem.getInput(GameController.ControlType.Show))
        {
            showIndicators = !showIndicators;
        }
    }
}
