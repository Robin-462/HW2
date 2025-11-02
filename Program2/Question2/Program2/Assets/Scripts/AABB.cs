using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ISTA 425 / INFO 525 Algorithms for Games
//
// Sample code file: A simple class to represent
// an axis-aligned bounding box (AABB)

public class AABB
{
    private BoxCollider2D box;

    // min and max bounds of the AABB
    private Vector2 min;
    private Vector2 max;

    // accessor methods of the AABB
    public Vector2 Min 
    {
        get { return min; }
    }
    public Vector2 Max
    {
        get { return max; }
    }

    public AABB ()
    {
        min = Vector2.zero;
        max = Vector2.zero;
    }

    // the AABB will inherit its extents from
    // the attached BoxCollider2D object
    public void SetCollider (BoxCollider2D box)
    {
        this.box = box;

        UpdateBounds();
    }

    // Update AABB (min, max) bounds relative to world space coordinates
    public void UpdateBounds ()
    {
        if (box == null)
        {
            //Debug.Log("Box is NULL!");
            return;
        }

        // a position relative to local (model) space coordinates
        Vector4 center = new Vector4 (box.offset.x, box.offset.y, 0, 1);
        Vector2 extent = box.size;

        // direction vectors in local (model) space coordinates
        Vector4 up     = new Vector4 (0,            extent.y / 2, 0, 0);
        Vector4 right  = new Vector4 (extent.x / 2, 0,            0, 0);

        // the transformation from local (model) space to world space 
        Transform t = box.gameObject.transform;
        Matrix4x4 toWorld = t.localToWorldMatrix;

        Vector4 result = new Vector4();

        // max value (NE)
        result = toWorld * (center + up + right);
        max.x = result.x;
        max.y = result.y;

        // min value (SW)
        result = toWorld * (center - up - right);
        min.x = result.x;
        min.y = result.y;
    }

    // Get the 4 vertices of AABB in world space coordinates (CCW order)
    public Vector3[] GetVertices ()
    {
        // vertices are defined in a CCW ordering
        Vector3[] vertices = new Vector3[4];

        // NE vertex
        vertices[0] = new Vector3 (Max.x, Max.y, 0);
        // NW vertex
        vertices[1] = new Vector3 (Min.x, Max.y, 0);
        // SW vertex
        vertices[2] = new Vector3 (Min.x, Min.y, 0);
        // SE vertex
        vertices[3] = new Vector3 (Max.x, Min.y, 0);

        return vertices;
    }
}
