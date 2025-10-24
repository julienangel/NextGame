using UnityEngine;

/// <summary>
/// Component for pieces that have directional movement restrictions
/// </summary>
public class DirectionalPiece : MonoBehaviour
{
    public Direction AllowedIn;
    public Direction AllowedOut;

    public bool CanEnterFrom(Direction direction)
    {
        return (AllowedIn & direction) != 0;
    }

    public bool CanExitTo(Direction direction)
    {
        return (AllowedOut & direction) != 0;
    }
}
