using System.Drawing;
using System.Numerics;
using Smash.Graphics;

public class Cube
{
    public static readonly Vector3[] Vertices =
    {
        new(-1, -1, -1),
        new( 1, -1, -1),
        new( 1,  1, -1),
        new(-1,  1, -1),

        new(-1, -1,  1),
        new( 1, -1,  1),
        new( 1,  1,  1),
        new(-1,  1,  1),
    };

    public static readonly int[] Indices =
    {
        4,5,6,
        4,6,7,

        0,2,1,
        0,3,2,

        0,4,7,
        0,7,3,

        1,2,6,
        1,6,5,

        3,7,6,
        3,6,2,

        0,1,5,
        0,5,4
    };

    public Texture2D Texture;

    public Cube(Texture2D texture)
    {
        Texture = texture; 
    }
}