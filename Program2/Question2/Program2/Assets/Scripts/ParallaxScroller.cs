// ISTA 425 / INFO 525 Algorithms for Games
//
// Sample code file

using UnityEngine;

public class ParallaxScroller : MonoBehaviour
{
    public GameObject parallaxCamera;
    public float parallaxLevel = 0.5f;
    public float playerMoveSpeed = 5f;

    private float startPos;
    private float layerWidth;
    private Camera cam;

    void Start()
    {
        startPos = transform.position.x;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        
        if (Mathf.Abs(horizontalInput) < 0.1f) 
            return;
        
        float scrollDistance = -horizontalInput * playerMoveSpeed * Time.deltaTime * parallaxLevel;
        Vector3 newPos = transform.position + new Vector3(scrollDistance, 0, 0);
        
        MakeBackgroundRepeat(ref newPos);
        
        transform.position = newPos;
    }

    void MakeBackgroundRepeat(ref Vector3 position)
    {
        if (cam == null) return;

        float camHalfWidth = cam.orthographicSize * cam.aspect;
        float camRightEdge = cam.transform.position.x + camHalfWidth;
        float camLeftEdge = cam.transform.position.x - camHalfWidth;

        float layerRightEdge = position.x + layerWidth / 2;
        float layerLeftEdge = position.x - layerWidth / 2;

        if (layerRightEdge < camLeftEdge)
        {
            position.x += layerWidth;
        }
        else if (layerLeftEdge > camRightEdge)
        {
            position.x -= layerWidth;
        }
    }
}