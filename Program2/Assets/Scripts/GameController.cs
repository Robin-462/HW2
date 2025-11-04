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
    private readonly List<CollidableObject> allObjects = new List<CollidableObject>();

    // this class is used internally to query and update inputs and
    // enforces a one to one mapping between input keys and system
    // functions.
    private class InputStatus
    {
        public KeyCode Key;
        public bool    Status;
    }
    
    // inputs for the x, y axes of player motion
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
        float  leftBound = -(halfLength - layerWidth / 2.0f - padding);
        float rightBound =  (halfLength - layerWidth / 2.0f - padding);

        if      (pos < leftBound)
            clampedPos = leftBound;
        else if (pos > rightBound)
            clampedPos = rightBound;
        else
            clampedPos = pos;

        return clampedPos;
    }

    public float GetAxis (AxisType axis)
    {
        return inputAxes[(int) axis];
    }

    public bool GetInput (ControlType type)
    {
        bool input = false;

        if (inputStatusDictionary.ContainsKey (type))
            input = inputStatusDictionary[type].Status;

        return input;
    }

    public void UpdateInput ()
    {
        inputAxes[0] = Input.GetAxisRaw("Horizontal");
        inputAxes[1] = Input.GetAxisRaw("Vertical");

        foreach (ControlType type in System.Enum.GetValues(typeof(ControlType)))
        {
            if (inputStatusDictionary.ContainsKey(type))
                inputStatusDictionary[type].Status = Input.GetKeyDown(inputStatusDictionary[type].Key);
        }
    }

    void Start()
    {
        GameObject foreground = GameObject.FindGameObjectWithTag("Foreground");
        layerWidth = foreground.GetComponent<SpriteRenderer>().bounds.size.x;

        scrollerMove = Vector3.zero;
        playerMove   = Vector3.zero;

        // initialize motion axes and 1:1 mapping of keycode to status
        inputAxes = Vector2.zero;
        inputStatusDictionary = new Dictionary<ControlType, InputStatus> ();
        foreach (InputMapping mapping in inputMappingArray)
        {
            if (!inputStatusDictionary.ContainsKey (mapping.type))
                inputStatusDictionary[mapping.type] = new InputStatus ();

            inputStatusDictionary[mapping.type].Key = mapping.key;
        }
        if (player != null)
        {
            AddObject(player);
        }
        foreach (GameObject raven in ravens)
        {
            if (raven != null) AddObject(raven);
        }
        foreach (GameObject fireball in fireballs)
        {
            if (fireball != null) AddObject(fireball);
        }
    }
    
    void AddObject(GameObject obj)
    {
        BoxCollider2D boxCollider = obj.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            AABB newAabb = CreateAABB(boxCollider);
            CollidableObject newObj = new CollidableObject 
            { 
                gameObject = obj, 
                AABB = newAabb 
            };
            allObjects.Add(newObj);
        }
    }
    
    void Update()
    {
        UpdateInput();

        if (GetInput(ControlType.Quit))
            Application.Quit();
        CheckCollisions();
    }
    
    void CheckCollisions()
    {
        if (!enableSweepAndPrune) return;
        foreach (CollidableObject obj in allObjects)
        {
            if (obj.gameObject != null)
            {
                obj.UpdateBounds();
            }
        }
        for (int i = 0; i < allObjects.Count; i++)
        {
            for (int j = i + 1; j < allObjects.Count; j++)
            {
                CollidableObject obj1 = allObjects[i];
                CollidableObject obj2 = allObjects[j];
                
                if (obj1.gameObject != null && obj2.gameObject != null)
                {
                    bool noOverlap = 
                        (obj1.e1 < obj2.b1) ||
                        (obj2.e1 < obj1.b1) ||
                        (obj1.AABB.Max.y < obj2.AABB.Min.y) ||
                        (obj2.AABB.Max.y < obj1.AABB.Min.y);
                        
                    if (!noOverlap)
                    {
                        Debug.Log($"Sweep & Prune Collision: {obj1.gameObject.name} vs {obj2.gameObject.name}");
                        if ((obj1.gameObject == player && IsEnemy(obj2.gameObject)) ||
                            (obj2.gameObject == player && IsEnemy(obj1.gameObject)))
                        {
                            StartCoroutine(FlashPlayerRed());
                        }
                    }
                }
            }
        }
    }
    IEnumerator FlashPlayerRed()
    {
        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.5f);
            spriteRenderer.color = originalColor;
        }
    }
    
    bool IsEnemy(GameObject obj)
    {
        return obj.CompareTag("Raven") || obj.CompareTag("Fireball");
    }
}