using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ISTA 425 / INFO 525 Algorithms for Games
//
// Sample code file: Credit to Parker Jones
// for developing the core of this script

public class BoxIndicator : MonoBehaviour
{
    [Tooltip("Display lifetime in seconds")]
    public float Lifetime = 0.75f;

    private AABB aabb;

    private LineRenderer line;

    private Vector3[] positions;
    
    private Color color;
    private float current;

    public void SetAABB(AABB aabb)
    {
        this.aabb = aabb;
    }

    private void UpdateColor(Color color)
    {
        line.startColor = color;
        line.endColor = color;
    } 

    private void Start()
    {
        line    = GetComponent<LineRenderer>();
        color   = line.startColor;
        current = Lifetime;
    }
    
    private void Update()
    {
        current -= Time.deltaTime;
        // the indicator (or its reference) has expired
        if (aabb == null || current <= 0)
        { 
            Destroy(gameObject);

            return;
        }

        // update the current world space position as
        // the object may have moved since last frame
        aabb.UpdateBounds();
        positions = aabb.GetVertices();

        // update the world position of the indicator
        if (positions != null)
        {
            for (var i = 0; i < positions.Length; i++)
            {
                var pos = positions[i];
                line.SetPosition(i, pos);
            }
        }

        var alpha = current / Lifetime;
        UpdateColor(new Color (color.r, color.g, color.b, alpha));
    }
}
