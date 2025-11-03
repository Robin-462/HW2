// ISTA 425 / INFO 525 Algorithms for Games
//
// Sample code file

using UnityEngine;
using System.Collections.Generic;

public class ParallaxScroller : MonoBehaviour
{
    public float parallaxLevel = 0.5f;
    public float playerMoveSpeed = 5f;
    public BackgroundManager backgroundManager;

    private float layerWidth;
    private Camera cam;
    private GameObject currentBackground;

    void Start()
    {
        cam = Camera.main;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            layerWidth = sr.bounds.size.x;
        }
        MakeRandomBackground();
    }

    void Update()
    {
        float input = Input.GetAxis("Horizontal");
        if (input == 0) return;
        
        float move = -input * playerMoveSpeed * Time.deltaTime * parallaxLevel;
        Vector3 newPos = transform.position + new Vector3(move, 0, 0);
        
        CheckLoop(ref newPos);
        transform.position = newPos;
    }

    void CheckLoop(ref Vector3 pos)
    {
        if (cam == null) return;

        float camWidth = cam.orthographicSize * cam.aspect;
        float camLeft = cam.transform.position.x - camWidth;
        float camRight = cam.transform.position.x + camWidth;

        float myLeft = pos.x - layerWidth / 2;
        float myRight = pos.x + layerWidth / 2;

        if (myRight < camLeft)
        {
            pos.x += layerWidth;
            MakeRandomBackground();
        }
        else if (myLeft > camRight)
        {
            pos.x -= layerWidth;
            MakeRandomBackground();
        }
    }

    void MakeRandomBackground()
    {
        if (backgroundManager == null) return;
        
        List<GameObject> variants = GetVariants();
        if (variants == null || variants.Count == 0) return;

        int randomIndex = Random.Range(0, variants.Count);
        GameObject variant = variants[randomIndex];
        
        if (variant != null)
        {
            if (currentBackground != null)
            {
                Destroy(currentBackground);
            }
            
            currentBackground = Instantiate(variant, transform);
            currentBackground.transform.localPosition = Vector3.zero;
        }
    }

    List<GameObject> GetVariants()
    {
        if (backgroundManager == null) return null;
        
        string objName = gameObject.name.ToLower();
        
        if (objName.Contains("10")) return backgroundManager.layer10Sky;
        if (objName.Contains("09")) return backgroundManager.layer09Forest;
        if (objName.Contains("08")) return backgroundManager.layer08Forest;
        if (objName.Contains("07")) return backgroundManager.layer07Forest;
        if (objName.Contains("06")) return backgroundManager.layer06Forest;
        if (objName.Contains("05")) return backgroundManager.layer05Particle;
        if (objName.Contains("04")) return backgroundManager.layer04Forest;
        if (objName.Contains("03")) return backgroundManager.layer03Particle;
        if (objName.Contains("02")) return backgroundManager.layer02Bushes;
        if (objName.Contains("01")) return backgroundManager.layer01Mist;
        
        return null;
    }
}