using System.Collections.Generic;
using UnityEngine;

public class CollisionSystem : MonoBehaviour
{
    [Header("Tags")]
    public string playerTag   = "Player";
    public string ravenTag    = "Raven";
    public string fireballTag = "Fireball";

    [Header("Animator")]
    public string dyingBool = "isDying"; 

    [Header("Timing")]
    public float refreshInterval = 0.25f;
    public float spawnGrace      = 0.50f;
    public int   confirmFrames   = 2;   

    private GameObject    wizard;
    private Animator      wizAnimator;
    private BoxCollider2D playerBC;
    private AABB          playerAabb = new AABB();

    private readonly List<BoxCollider2D> ravenBCs    = new();
    private readonly List<BoxCollider2D> fireballBCs = new();

    private float nextRefreshAt;
    private float startTime;
    private bool  dead;

    private readonly Dictionary<Collider2D, int> overlapFrames = new();
    private List<Collider2D> _toClear;

    private AABB _tmpA = new AABB();
    private AABB _tmpB = new AABB();

    void Awake() { startTime = Time.time; }

    void Start()
    {
        wizard = GameObject.FindGameObjectWithTag(playerTag);
        if (!wizard)
        {
            Debug.LogError("[CollisionSystem] Player (tag) not found");
            enabled = false; return;
        }

        wizAnimator = wizard.GetComponent<Animator>();
        playerBC    = wizard.GetComponent<BoxCollider2D>();
        if (!playerBC)
        {
            Debug.LogError("[CollisionSystem] Player missing BoxCollider2D");
            enabled = false; return;
        }

        playerAabb.SetCollider(playerBC);
        RefreshLists(firstTime: true);
    }

    void Update()
    {
        if (Time.time >= nextRefreshAt) RefreshLists(false);
    }

    void LateUpdate()
    {
        if (Time.time - startTime < spawnGrace) return;

        if (!dead)
        {
            playerAabb.UpdateBounds();

            var touchedThisFrame = new HashSet<Collider2D>();

            for (int i = 0; i < ravenBCs.Count; i++)
            {
                var bc = ravenBCs[i];
                if (!bc || !bc.enabled || !bc.gameObject.activeInHierarchy) continue;

                _tmpA.SetCollider(bc); _tmpA.UpdateBounds();
                if (Overlap(playerAabb, _tmpA))
                {
                    touchedThisFrame.Add(bc);
                    BumpAndCheck(bc, "Raven");
                    if (dead) return;
                }
            }

            for (int i = 0; i < fireballBCs.Count; i++)
            {
                var bc = fireballBCs[i];
                if (!bc || !bc.enabled || !bc.gameObject.activeInHierarchy) continue;

                _tmpA.SetCollider(bc); _tmpA.UpdateBounds();
                if (Overlap(playerAabb, _tmpA))
                {
                    touchedThisFrame.Add(bc);
                    BumpAndCheck(bc, "Fireball");
                    if (dead) return;
                }
            }
            if (overlapFrames.Count > 0)
            {
                _toClear ??= new List<Collider2D>();
                _toClear.Clear();
                foreach (var kv in overlapFrames)
                    if (!touchedThisFrame.Contains(kv.Key))
                        _toClear.Add(kv.Key);
                foreach (var c in _toClear)
                    overlapFrames.Remove(c);
            }
        }

        for (int i = 0; i < fireballBCs.Count; i++)
        {
            var fbc = fireballBCs[i];
            if (!fbc || !fbc.enabled || !fbc.gameObject.activeInHierarchy) continue;

            _tmpA.SetCollider(fbc); _tmpA.UpdateBounds();

            for (int j = 0; j < ravenBCs.Count; j++)
            {
                var rbc = ravenBCs[j];
                if (!rbc || !rbc.enabled || !rbc.gameObject.activeInHierarchy) continue;

                _tmpB.SetCollider(rbc); _tmpB.UpdateBounds();

                if (Overlap(_tmpA, _tmpB))
                {
                    var rd = rbc.GetComponentInParent<RavenDeath>();
                    if (rd != null)
                    {
                        float dir = Mathf.Sign(fbc.transform.lossyScale.x);
                        if (dir == 0) dir = 1f;
                        rd.Die(dir);
                    }
                }
            }
        }
    }

    void BumpAndCheck(Collider2D col, string cause)
    {
        int n = 0;
        overlapFrames.TryGetValue(col, out n);
        n++;
        overlapFrames[col] = n;

        if (n >= confirmFrames)
        {
            Debug.Log($"Wizard hit by {cause}! (by {col.gameObject.name})");
            KillWizard(cause);
        }
    }

    void RefreshLists(bool firstTime)
    {
        ravenBCs.Clear();
        fireballBCs.Clear();

        var ravens = GameObject.FindGameObjectsWithTag(ravenTag);
        if (ravens != null)
        {
            foreach (var go in ravens)
            {
                var bc = go ? go.GetComponent<BoxCollider2D>() : null;
                if (bc) ravenBCs.Add(bc);
            }
        }

        var fires = GameObject.FindGameObjectsWithTag(fireballTag);
        if (fires != null)
        {
            foreach (var go in fires)
            {
                var bc = go ? go.GetComponent<BoxCollider2D>() : null;
                if (bc) fireballBCs.Add(bc);
            }
        }

        nextRefreshAt = Time.time + refreshInterval;

        if (firstTime)
            Debug.Log($"[CollisionSystem] Ready: Ravens={ravenBCs.Count}, Fireballs={fireballBCs.Count}");
    }

    static bool Overlap(AABB a, AABB b)
    {
        return a.Min.x < b.Max.x && a.Max.x > b.Min.x &&
               a.Min.y < b.Max.y && a.Max.y > b.Min.y;
    }

    void KillWizard(string cause)
    {
        if (dead) return;
        dead = true;

        Debug.Log($"Wizard hit by {cause}!");

        if (wizAnimator)
        {
            wizAnimator.ResetTrigger("isRunning");
            wizAnimator.ResetTrigger("isIdling");
            wizAnimator.ResetTrigger("isJumping");
            wizAnimator.ResetTrigger("isFalling");
            wizAnimator.ResetTrigger("isAttacking");

            if (!string.IsNullOrEmpty(dyingBool))
                wizAnimator.SetBool(dyingBool, true);
        }

        var rb = wizard.GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;

        var pc = wizard.GetComponent<PlayerController>();
        if (pc) pc.enabled = false;
    }
}
