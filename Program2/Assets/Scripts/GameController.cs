// ISTA 425 / INFO 525 Algorithms for Games
//
// Sample code file

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public enum AxisType
    {
        X,
        Y
    }

    public enum ControlType
    {
        Cast,
        Hack,
        Jump,
        Die,
        Quit,
        Show
    }

    [System.Serializable]
    public class InputMapping
    {
        [Tooltip("Game control type")]
        public ControlType type;
        [Tooltip("System key code")]
        public KeyCode key;
    }

    [System.Serializable]
    public class CollidableObject
    {
        public GameObject gameObject;
        public AABB AABB;
        public float b1;
        public float e1;
        
        public void UpdateBounds()
        {
            if (AABB != null)
            {
                AABB.UpdateBounds();
                b1 = AABB.Min.x;
                e1 = AABB.Max.x;
            }
        }
    }

    [System.Serializable]
    public class IntervalPoint
    {
        public CollidableObject obj;
        public float value;
        public bool isBegin;
    }

    [Tooltip("Array of input mappings to player action types")]
    public InputMapping[] inputMappingArray;
    public BoxIndicator indicatorPrefab;
    
    [Tooltip("Horizontal tiles of background (background width)")]
    public int numTiles = 3;

    // this is a fudge factor because the tiles are not exactly equal
    // to the camera width and I don't feel like setting a new pixel 
    // scale, reimporting and realigning all of the background layers.
    [Tooltip("Boundary padding when background doesn't exactly match camera FOV")]
    public float padding = 0.8f;

    public Vector3 scrollerMove;
    public Vector3 playerMove;
    public bool enableSweepAndPrune = true;
    public GameObject player;
    public List<GameObject> ravens = new List<GameObject>();
    public List<GameObject> fireballs = new List<GameObject>();

    private float layerWidth;
    private readonly List<CollidableObject> collidableObjects = new List<CollidableObject>();
    private readonly List<GameObject> objectsToAdd = new List<GameObject>();
    private readonly List<GameObject> objectsToRemove = new List<GameObject>();

    // this class is used internally to query and update inputs and
    // enforces a one to one mapping between input keys and system
    // functions.
    private class InputStatus
    {
        public KeyCode Key;
        public bool Status;
    }
    
    private Vector2 inputAxes;
    // dictionary of all over valid input types
    private Dictionary<ControlType, InputStatus> inputStatusDictionary;

    public AABB CreateAABB(BoxCollider2D box)
    {
        var aabb = new AABB();
        aabb.SetCollider(box);
        return aabb;
    }

    // This method creates a visual indicator for a 2D box collider.
    public BoxIndicator CreateIndicator(AABB aabb)
    {
        var indicator = Instantiate(indicatorPrefab);
        indicator.SetAABB(aabb);
        return indicator;
    }

    // This method may be helpful to map player position to valid scrolling
    // range. Prevents player from leaving the left or right side of a map
    // as per clamp algorithm given in class (see GPAT Ch. 2).
    public float Clamp(float pos)
    {
        float clampedPos;
        // equal to half the full length of the tiles, (n * width) / 2
        float halfLength = numTiles * layerWidth / 2.0f;
        // the left and right bounds minus the half screen padding area
        float leftBound = -(halfLength - layerWidth / 2.0f - padding);
        float rightBound = halfLength - layerWidth / 2.0f - padding;

        if (pos < leftBound)
            clampedPos = leftBound;
        else if (pos > rightBound)
            clampedPos = rightBound;
        else
            clampedPos = pos;

        return clampedPos;
    }

    public float GetAxis(AxisType axis)
    {
        return inputAxes[(int)axis];
    }

    public bool getInput(ControlType type)
    {
        if (inputStatusDictionary.TryGetValue(type, out InputStatus status))
            return status.Status;

        return false;
    }

    private void UpdateInput()
    {
        inputAxes[0] = Input.GetAxisRaw("Horizontal");
        inputAxes[1] = Input.GetAxisRaw("Vertical");

        foreach (ControlType type in System.Enum.GetValues(typeof(ControlType)))
        {
            if (inputStatusDictionary.TryGetValue(type, out InputStatus status))
                status.Status = Input.GetKeyDown(status.Key);
        }
    }

    void InitializeSweepAndPrune()
    {
        collidableObjects.Clear();
        
        if (player != null)
        {
            AddCollidableObject(player);
        }
    }
    
    void AddCollidableObject(GameObject obj)
    {
        BoxCollider2D boxCollider = obj.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            AABB newAabb = CreateAABB(boxCollider);
            CollidableObject collidable = new CollidableObject 
            { 
                gameObject = obj, 
                AABB = newAabb 
            };
            collidableObjects.Add(collidable);
        }
    }
    
    void PerformSweepAndPrune()
    {
        if (!enableSweepAndPrune) return;
        
        ProcessObjectChanges();
        
        foreach (CollidableObject obj in collidableObjects)
        {
            if (obj.gameObject != null && obj.gameObject.activeInHierarchy)
            {
                obj.UpdateBounds();
            }
        }
        
        List<IntervalPoint> masterList = new List<IntervalPoint>();
        
        foreach (CollidableObject obj in collidableObjects)
        {
            if (obj.gameObject != null && obj.gameObject.activeInHierarchy)
            {
                masterList.Add(new IntervalPoint { 
                    obj = obj, 
                    value = obj.b1, 
                    isBegin = true 
                });
                masterList.Add(new IntervalPoint { 
                    obj = obj, 
                    value = obj.e1, 
                    isBegin = false 
                });
            }
        }
        
        InsertionSort(masterList);
        
        List<CollidableObject> activeList = new List<CollidableObject>();
        HashSet<string> checkedPairs = new HashSet<string>();
        
        foreach (IntervalPoint point in masterList)
        {
            if (point.isBegin)
            {
                foreach (CollidableObject activeObj in activeList)
                {
                    if (activeObj != point.obj)
                    {
                        string pairKey = GetPairKey(point.obj.gameObject, activeObj.gameObject);
                        if (checkedPairs.Add(pairKey))
                        {
                            if (AABBIntersectionTest(point.obj, activeObj))
                            {
                                Debug.Log($"Sweep & Prune Collision: {point.obj.gameObject.name} vs {activeObj.gameObject.name}");
                                VisualizeCollision(point.obj.gameObject, activeObj.gameObject);
                            }
                        }
                    }
                }
                activeList.Add(point.obj);
            }
            else
            {
                activeList.Remove(point.obj);
            }
        }
    }
    
    void InsertionSort(List<IntervalPoint> list)
    {
        for (int i = 1; i < list.Count; i++)
        {
            IntervalPoint current = list[i];
            int j = i - 1;
            
            while (j >= 0 && list[j].value > current.value)
            {
                list[j + 1] = list[j];
                j--;
            }
            list[j + 1] = current;
        }
    }
    
    bool AABBIntersectionTest(CollidableObject a, CollidableObject b)
    {
        bool noOverlap = 
            (a.e1 < b.b1) ||
            (b.e1 < a.b1) ||
            (a.AABB.Max.y < b.AABB.Min.y) ||
            (b.AABB.Max.y < a.AABB.Min.y);
            
        return !noOverlap;
    }
    
    string GetPairKey(GameObject a, GameObject b)
    {
        int id1 = a.GetInstanceID();
        int id2 = b.GetInstanceID();
        return id1 < id2 ? $"{id1}-{id2}" : $"{id2}-{id1}";
    }
    
    void VisualizeCollision(GameObject a, GameObject b)
    {
        StartCoroutine(FlashObjectCoroutine(a));
        StartCoroutine(FlashObjectCoroutine(b));
    }
    
    IEnumerator FlashObjectCoroutine(GameObject obj)
    {
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = original;
        }
    }
    
    void ProcessObjectChanges()
    {
        foreach (GameObject obj in objectsToAdd)
        {
            AddCollidableObject(obj);
        }
        objectsToAdd.Clear();
        
        foreach (GameObject obj in objectsToRemove)
        {
            collidableObjects.RemoveAll(o => o.gameObject == obj);
        }
        objectsToRemove.Clear();
    }

    private void AddObjectToSweepAndPrune(GameObject obj)
    {
        if (obj != null)
        {
            objectsToAdd.Add(obj);
        }
    }
    
    public void RemoveObjectFromSweepAndPrune(GameObject obj)
    {
        if (obj != null)
        {
            objectsToRemove.Add(obj);
        }
    }

    void AutoAddEnemies()
    {
        GameObject[] sceneRavens = GameObject.FindGameObjectsWithTag("Raven");
        GameObject[] sceneFireballs = GameObject.FindGameObjectsWithTag("Fireball");
        
        foreach (GameObject raven in sceneRavens)
        {
            if (!IsObjectInSystem(raven))
            {
                AddObjectToSweepAndPrune(raven);
            }
        }
        
        foreach (GameObject fireball in sceneFireballs)
        {
            if (!IsObjectInSystem(fireball))
            {
                AddObjectToSweepAndPrune(fireball);
            }
        }
    }

    bool IsObjectInSystem(GameObject obj)
    {
        foreach (CollidableObject collidable in collidableObjects)
        {
            if (collidable.gameObject == obj)
                return true;
        }
        return false;
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject foreground = GameObject.FindGameObjectWithTag("Foreground");
        layerWidth = foreground.GetComponent<SpriteRenderer>().bounds.size.x;

        scrollerMove = Vector3.zero;
        playerMove = Vector3.zero;

        // initialize motion axes and 1:1 mapping of keycode to status
        inputAxes = Vector2.zero;
        inputStatusDictionary = new Dictionary<ControlType, InputStatus>();
        foreach (InputMapping mapping in inputMappingArray)
        {
            if (!inputStatusDictionary.ContainsKey(mapping.type))
                inputStatusDictionary[mapping.type] = new InputStatus();

            inputStatusDictionary[mapping.type].Key = mapping.key;
        }

        InitializeSweepAndPrune();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateInput();

        if (getInput(ControlType.Quit))
            Application.Quit();

        AutoAddEnemies();
        PerformSweepAndPrune();
    }
}