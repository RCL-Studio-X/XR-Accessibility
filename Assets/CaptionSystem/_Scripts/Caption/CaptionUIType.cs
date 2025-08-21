using UnityEngine;

/// <summary>
/// Defines different types of caption UI behaviors
/// </summary>
public enum CaptionUIType
{
    [Tooltip("Caption follows the player/camera smoothly")]
    PlayerFollow,

    [Tooltip("Caption is positioned relative to a static object (like a TV)")]
    StaticObject,

    [Tooltip("Caption appears as dialogue bubble above characters")]
    CharacterDialogue,

    [Tooltip("Caption appears in screen space UI")]
    ScreenSpace,

    [Tooltip("Custom behavior - uses prefab's existing behavior components")]
    Custom
}